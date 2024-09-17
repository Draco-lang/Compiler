using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Draco.Coverage;

/// <summary>
/// Weaves in the instrumentation code into an assembly.
/// </summary>
internal sealed class InstrumentationWeaver
{
    /// <summary>
    /// Weaves the instrumentation code into the specified assembly.
    /// </summary>
    /// <param name="assemblyLocation">The location of the assembly to weave.</param>
    /// <param name="targetLocation">The location to save the weaved assembly.</param>
    public static void WeaveInstrumentationCode(
        string assemblyLocation,
        string targetLocation,
        InstrumentationWeaverSettings? settings = null)
    {
        settings ??= InstrumentationWeaverSettings.Default;

        var readerParameters = new ReaderParameters { ReadSymbols = true };
        using var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyLocation, readerParameters);

        var weaver = new InstrumentationWeaver(assemblyDefinition.MainModule, settings);
        weaver.WeaveModule();

        assemblyDefinition.Write(targetLocation);
    }

    private readonly InstrumentationWeaverSettings settings;
    private readonly ModuleDefinition weavedModule;

    private InstrumentationWeaver(ModuleDefinition weavedModule, InstrumentationWeaverSettings settings)
    {
        this.settings = settings;
        this.weavedModule = weavedModule;
    }

    private void WeaveModule()
    {
        foreach (var type in this.weavedModule.GetTypes()) this.WeaveType(type);
    }

    private void WeaveType(TypeDefinition type)
    {
        if (this.SkipType(type)) return;
        foreach (var method in type.Methods) this.WeaveMethod(method);
    }

    private void WeaveMethod(MethodDefinition method)
    {
        if (method.IsAbstract || method.IsPInvokeImpl || method.IsRuntime || !method.HasBody) return;
        if (this.SkipMethod(method)) return;

        method.Body.SimplifyMacros();

        var jumpTargetPatches = new Dictionary<int, Instruction>();
        var ilProcessor = method.Body.GetILProcessor();

        // Weave each instruction for instrumentation
        var sequencePoints = method.DebugInformation.SequencePoints;
        foreach (var sequencePoint in sequencePoints)
        {
            var instruction = this.WeaveInstruction(ilProcessor, sequencePoint);
            if (instruction is not null) jumpTargetPatches.Add(sequencePoint.Offset, instruction);
        }

        // Patch jump targets
        foreach (var instruction in ilProcessor.Body.Instructions)
        {
            this.PatchJumpTarget(instruction, jumpTargetPatches);
        }

        // Patch exception handlers
        foreach (var exceptionHandler in method.Body.ExceptionHandlers)
        {
            this.PatchExceptionHandler(exceptionHandler, jumpTargetPatches);
        }

        method.Body.OptimizeMacros();
    }

    private Instruction? WeaveInstruction(ILProcessor ilProcessor, SequencePoint sequencePoint)
    {
        if (sequencePoint.IsHidden) return null;

        // Get the instruction at the sequence point
        var instruction = ilProcessor.Body.Instructions.FirstOrDefault(i => i.Offset == sequencePoint.Offset);
        if (instruction is null) return null;

        // Insert the instrumentation code before the instruction
        return this.AddInstrumentationCode(ilProcessor, instruction, sequencePoint);
    }

    private Instruction AddInstrumentationCode(ILProcessor ilProcessor, Instruction before, SequencePoint sequencePoint)
    {
        // For now we just write a message to the console
        var message = $"Sequence point at {sequencePoint.Document.Url}:{sequencePoint.StartLine}";
        var writeLineMethod = this.weavedModule.ImportReference(typeof(System.Console).GetMethod("WriteLine", [typeof(string)]));
        var instructions = new[]
        {
            Instruction.Create(OpCodes.Ldstr, message),
            Instruction.Create(OpCodes.Call, writeLineMethod),
        };
        foreach (var instruction in instructions) ilProcessor.InsertBefore(before, instruction);
        return instructions[0];
    }

    private void PatchExceptionHandler(ExceptionHandler exceptionHandler, IReadOnlyDictionary<int, Instruction> jumpTargetPatches)
    {
        this.PatchJumpTarget(exceptionHandler.TryStart, jumpTargetPatches);
        this.PatchJumpTarget(exceptionHandler.TryEnd, jumpTargetPatches);
        this.PatchJumpTarget(exceptionHandler.HandlerStart, jumpTargetPatches);
        this.PatchJumpTarget(exceptionHandler.HandlerEnd, jumpTargetPatches);
        this.PatchJumpTarget(exceptionHandler.FilterStart, jumpTargetPatches);
    }

    private void PatchJumpTarget(Instruction instruction, IReadOnlyDictionary<int, Instruction> jumpTargetPatches)
    {
        if (instruction.Operand is Instruction targetInstruction)
        {
            if (jumpTargetPatches.TryGetValue(targetInstruction.Offset, out var newTarget))
            {
                instruction.Operand = newTarget;
            }
        }
        else if (instruction.Operand is Instruction[] targetInstructions)
        {
            for (var i = 0; i < targetInstructions.Length; ++i)
            {
                if (jumpTargetPatches.TryGetValue(targetInstructions[i].Offset, out var newTarget))
                {
                    targetInstructions[i] = newTarget;
                }
            }
        }
    }

    private bool SkipType(TypeDefinition type) => this.ShouldExcludeByAttributes(type.CustomAttributes);

    private bool SkipMethod(MethodDefinition method) => this.ShouldExcludeByAttributes(method.CustomAttributes);

    private bool ShouldExcludeByAttributes(IEnumerable<CustomAttribute> attributes) =>
           this.settings.CheckForExcludeCoverageAttribute && attributes.Any(IsExcludeCoverageAttribute)
        || this.settings.CheckForCompilerGeneratedAttribute && attributes.Any(IsCompilerGeneratedAttribute);

    private static bool IsExcludeCoverageAttribute(CustomAttribute attribute) =>
        attribute.AttributeType.FullName == typeof(ExcludeFromCodeCoverageAttribute).FullName;

    private static bool IsCompilerGeneratedAttribute(CustomAttribute attribute) =>
        attribute.AttributeType.FullName == typeof(CompilerGeneratedAttribute).FullName;
}
