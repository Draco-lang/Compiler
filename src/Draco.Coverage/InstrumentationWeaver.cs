using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Draco.Coverage;

internal sealed class InstrumentationWeaver
{
    public static void WeaveInstrumentationCode(string assemblyLocation)
    {
        var readerParameters = new ReaderParameters { ReadSymbols = true };
        using var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyLocation, readerParameters);
        var weaver = new InstrumentationWeaver(assemblyDefinition.MainModule);
        weaver.WeaveModule();
        assemblyDefinition.Write(assemblyLocation);
    }

    private readonly ModuleDefinition weavedModule;

    private InstrumentationWeaver(ModuleDefinition weavedModule)
    {
        this.weavedModule = weavedModule;
    }

    private void WeaveModule()
    {
        foreach (var type in this.weavedModule.GetTypes()) this.WeaveType(type);
    }

    private void WeaveType(TypeDefinition type)
    {
        foreach (var method in type.Methods) this.WeaveMethod(method);
    }

    private void WeaveMethod(MethodDefinition method)
    {
        if (method.IsAbstract || method.IsPInvokeImpl || method.IsRuntime || !method.HasBody) return;

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
}
