using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
    /// <param name="sourcePath">The path to the assembly that should be weaved.</param>
    /// <param name="targetPath">The path to write the weaved assembly to.</param>
    /// <param name="settings">The settings for the weaver.</param>
    public static void WeaveInstrumentationCode(
        string sourcePath,
        string targetPath,
        InstrumentationWeaverSettings? settings = null)
    {
        var readerParameters = new ReaderParameters { ReadSymbols = true };
        using var targetStream = new MemoryStream();
        // NOTE: We don't fall-back to the stream-based method here, because Cecil needs the path of the assembly to read the symbols
        using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(sourcePath, readerParameters))
        {
            WeaveInstrumentationCode(assemblyDefinition, targetStream, settings);
        }

        File.WriteAllBytes(targetPath, targetStream.ToArray());
    }

    /// <summary>
    /// Weaves the instrumentation code into the specified assembly.
    /// </summary>
    /// <param name="sourceStream">The stream to read the assembly from that should be weaved.</param>
    /// <param name="targetStream">The stream to write the weaved assembly.</param>
    public static void WeaveInstrumentationCode(
        Stream sourceStream,
        Stream targetStream,
        InstrumentationWeaverSettings? settings = null)
    {
        var readerParameters = new ReaderParameters { ReadSymbols = true };
        using var assemblyDefinition = AssemblyDefinition.ReadAssembly(sourceStream, readerParameters);

        WeaveInstrumentationCode(assemblyDefinition, targetStream, settings);
    }

    private static void WeaveInstrumentationCode(
        AssemblyDefinition assemblyDefinition,
        Stream targetStream,
        InstrumentationWeaverSettings? settings = null)
    {
        settings ??= InstrumentationWeaverSettings.Default;

        var weaver = new InstrumentationWeaver(assemblyDefinition.MainModule, settings);
        weaver.WeaveModule();

        assemblyDefinition.Write(targetStream);
    }

    private readonly InstrumentationWeaverSettings settings;
    private readonly ModuleDefinition weavedModule;
    private readonly List<Mono.Cecil.Cil.SequencePoint> recordedSequencePoints = [];
    private MethodDefinition? allocateHitsMethod;
    private MethodDefinition? recordHitMethod;
    private MethodDefinition? collectorInitializerMethod;

    private InstrumentationWeaver(ModuleDefinition weavedModule, InstrumentationWeaverSettings settings)
    {
        this.settings = settings;
        this.weavedModule = weavedModule;
    }

    private void WeaveModule()
    {
        this.InjectCoverageCollector();
        foreach (var type in this.weavedModule.GetTypes()) this.WeaveType(type);
        this.InjectCoverageCollectorInitializer();
    }

    private void InjectCoverageCollector()
    {
        var templateAssembly = typeof(CoverageCollector).Assembly;
        using var templateAssemblyDefitition = AssemblyDefinition.ReadAssembly(templateAssembly.Location);

        // Reference the assembly
        this.weavedModule.AssemblyReferences.Add(AssemblyNameReference.Parse(templateAssembly.FullName));

        // Collector class

        var collectorTemplate = templateAssemblyDefitition.MainModule.GetType(typeof(CoverageCollector).FullName);
        var collectorType = CloneType(collectorTemplate);

        // Collector custom attributes

        foreach (var customAttrib in collectorTemplate.CustomAttributes)
        {
            collectorType.CustomAttributes.Add(CloneCustomAttribute(customAttrib));
        }

        // Collector fields

        foreach (var fieldTemplate in collectorTemplate.Fields)
        {
            collectorType.Fields.Add(CloneField(fieldTemplate));
        }

        // Collector methods

        foreach (var methodTemplate in collectorTemplate.Methods)
        {
            var method = CloneMethod(methodTemplate);
            foreach (var parameterTemplate in methodTemplate.Parameters)
            {
                method.Parameters.Add(CloneParameter(parameterTemplate));
            }
            collectorType.Methods.Add(method);

            if (method.Name == ".cctor")
            {
                // This will be written at the end of weaving
                this.collectorInitializerMethod = method;
                continue;
            }

            var ilProcessor = method.Body.GetILProcessor();

            // Locals
            foreach (var local in methodTemplate.Body.Variables)
            {
                method.Body.Variables.Add(CloneVariable(local));
            }

            // Instructions
            foreach (var instruction in methodTemplate.Body.Instructions)
            {
                ilProcessor.Append(CloneInstruction(instruction, collectorType));
            }

            if (methodTemplate.Name == nameof(CoverageCollector.AllocateHits))
            {
                this.allocateHitsMethod = method;
            }
            else if (methodTemplate.Name == nameof(CoverageCollector.RecordHit))
            {
                this.recordHitMethod = method;
            }
        }

        // Add the class to the target module

        this.weavedModule.Types.Add(collectorType);

        CustomAttribute CloneCustomAttribute(CustomAttribute customAttribute) => new(
            this.weavedModule.ImportReference(customAttribute.Constructor),
            customAttribute.GetBlob());

        TypeDefinition CloneType(TypeDefinition typeDefinition) => new(
            @namespace: typeDefinition.Namespace,
            name: typeDefinition.Name,
            attributes: typeDefinition.Attributes,
            baseType: this.weavedModule.ImportReference(typeDefinition.BaseType));

        FieldDefinition CloneField(FieldDefinition fieldDefinition) => new(
            name: fieldDefinition.Name,
            attributes: fieldDefinition.Attributes,
            fieldType: this.weavedModule.ImportReference(fieldDefinition.FieldType));

        ParameterDefinition CloneParameter(ParameterDefinition parameterDefinition) => new(
            name: parameterDefinition.Name,
            attributes: parameterDefinition.Attributes,
            parameterType: this.weavedModule.ImportReference(parameterDefinition.ParameterType));

        MethodDefinition CloneMethod(MethodDefinition methodDefinition) => new(
            name: methodDefinition.Name,
            attributes: methodDefinition.Attributes,
            returnType: this.weavedModule.ImportReference(methodDefinition.ReturnType));

        VariableDefinition CloneVariable(VariableDefinition variableDefinition) => new(
            variableType: this.weavedModule.ImportReference(variableDefinition.VariableType));

        Instruction CloneInstruction(Instruction instruction, TypeDefinition typeDefinition)
        {
            if (instruction.Operand is FieldDefinition fieldDefinition)
            {
                return Instruction.Create(instruction.OpCode, typeDefinition.Fields.First(f => f.Name == fieldDefinition.Name));
            }
            if (instruction.Operand is TypeReference typeReference)
            {
                return Instruction.Create(instruction.OpCode, this.weavedModule.ImportReference(typeReference));
            }
            if (instruction.Operand is MethodReference methodReference)
            {
                return Instruction.Create(instruction.OpCode, this.weavedModule.ImportReference(methodReference));
            }
            return instruction;
        }
    }

    private void InjectCoverageCollectorInitializer()
    {
        if (this.collectorInitializerMethod is null) throw new InvalidOperationException("collector initializer method was not declared");

        var collectorType = this.collectorInitializerMethod.DeclaringType;
        var ilProcessor = this.collectorInitializerMethod.Body.GetILProcessor();

        var sequencePointType = typeof(SequencePoint);
        var sequencePointTypeRef = this.weavedModule.ImportReference(sequencePointType);
        var sequencePointCtor = this.weavedModule.ImportReference(sequencePointType.GetConstructors().First(c => c.GetParameters().Length > 0));

        // Hits = AllocateHits(sequencePoints.Length);
        var hitsField = collectorType.Fields.First(f => f.Name == nameof(CoverageCollector.Hits));
        ilProcessor.Append(Instruction.Create(OpCodes.Ldc_I4, this.recordedSequencePoints.Count));
        ilProcessor.Append(Instruction.Create(OpCodes.Call, this.allocateHitsMethod));
        ilProcessor.Append(Instruction.Create(OpCodes.Stsfld, hitsField));

        // SequencePoints = new SequencePoint[sequencePoints.Length];
        var sequencePointsField = collectorType.Fields.First(f => f.Name == nameof(CoverageCollector.SequencePoints));
        ilProcessor.Append(Instruction.Create(OpCodes.Ldc_I4, this.recordedSequencePoints.Count));
        ilProcessor.Append(Instruction.Create(OpCodes.Newarr, sequencePointTypeRef));
        ilProcessor.Append(Instruction.Create(OpCodes.Stsfld, sequencePointsField));
        for (var i = 0; i < this.recordedSequencePoints.Count; ++i)
        {
            // SequencePoints[i] = new SequencePoint(...);
            var sequencePoint = this.recordedSequencePoints[i];
            ilProcessor.Append(Instruction.Create(OpCodes.Ldsfld, sequencePointsField));
            ilProcessor.Append(Instruction.Create(OpCodes.Ldc_I4, i));
            ilProcessor.Append(Instruction.Create(OpCodes.Ldstr, sequencePoint.Document.Url));
            ilProcessor.Append(Instruction.Create(OpCodes.Ldc_I4, sequencePoint.Offset));
            ilProcessor.Append(Instruction.Create(OpCodes.Ldc_I4, sequencePoint.StartLine));
            ilProcessor.Append(Instruction.Create(OpCodes.Ldc_I4, sequencePoint.StartColumn));
            ilProcessor.Append(Instruction.Create(OpCodes.Ldc_I4, sequencePoint.EndLine));
            ilProcessor.Append(Instruction.Create(OpCodes.Ldc_I4, sequencePoint.EndColumn));
            ilProcessor.Append(Instruction.Create(OpCodes.Newobj, sequencePointCtor));
            ilProcessor.Append(Instruction.Create(OpCodes.Stelem_Any, sequencePointTypeRef));
        }

        // return
        ilProcessor.Append(Instruction.Create(OpCodes.Ret));
    }

    private void WeaveType(TypeDefinition type)
    {
        if (this.SkipType(type)) return;
        foreach (var method in type.Methods) this.WeaveMethod(method);
    }

    private void WeaveMethod(MethodDefinition method)
    {
        if (method.IsAbstract || method.IsPInvokeImpl || method.IsRuntime || method.IsRuntimeSpecialName || !method.HasBody) return;
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

    private Instruction? WeaveInstruction(ILProcessor ilProcessor, Mono.Cecil.Cil.SequencePoint sequencePoint)
    {
        if (sequencePoint.IsHidden) return null;

        // Get the instruction at the sequence point
        var instruction = ilProcessor.Body.Instructions.FirstOrDefault(i => i.Offset == sequencePoint.Offset);
        if (instruction is null) return null;

        // Insert the instrumentation code before the instruction
        return this.AddInstrumentationCode(ilProcessor, instruction, sequencePoint);
    }

    private Instruction AddInstrumentationCode(ILProcessor ilProcessor, Instruction before, Mono.Cecil.Cil.SequencePoint sequencePoint)
    {
        var sequencePointIndex = this.recordedSequencePoints.Count;
        // Record the sequence point, so it can later be injected into the collector
        this.recordedSequencePoints.Add(sequencePoint);
        // Call record hit
        var instructions = new[]
        {
            Instruction.Create(OpCodes.Ldc_I4, sequencePointIndex),
            Instruction.Create(OpCodes.Call, this.recordHitMethod),
        };
        foreach (var instruction in instructions) ilProcessor.InsertBefore(before, instruction);
        return instructions[0];
    }

    private void PatchExceptionHandler(ExceptionHandler exceptionHandler, IReadOnlyDictionary<int, Instruction> jumpTargetPatches)
    {
        exceptionHandler.TryStart = GetPatchedJumpTarget(exceptionHandler.TryStart);
        exceptionHandler.TryEnd = GetPatchedJumpTarget(exceptionHandler.TryEnd);
        exceptionHandler.HandlerStart = GetPatchedJumpTarget(exceptionHandler.HandlerStart);
        exceptionHandler.HandlerEnd = GetPatchedJumpTarget(exceptionHandler.HandlerEnd);
        exceptionHandler.FilterStart = GetPatchedJumpTarget(exceptionHandler.FilterStart);

        Instruction? GetPatchedJumpTarget(Instruction? instruction)
        {
            if (instruction is null) return null;
            return jumpTargetPatches.TryGetValue(instruction.Offset, out var newTarget)
                ? newTarget
                : instruction;
        }
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
