
using System.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Draco.Instrumentator;

internal class Program
{
    static void Main(string[] args)
    {
        var dllPath = @"C:\dev\Compiler\src\artifacts\bin\Draco.Compiler.Tests\debug\Draco.Compiler.dll";
        using var peStream = File.Open(dllPath, FileMode.Open);
        using var module = ModuleDefinition.ReadModule(peStream);
        var resultType = new TypeDefinition("Draco.Instrumentation", "InstrumentationResult", TypeAttributes.Public | TypeAttributes.Sealed, module.TypeSystem.Object);
        var fieldResult = new FieldDefinition("Bits", FieldAttributes.Public | FieldAttributes.Static, module.TypeSystem.Int32.MakeArrayType());
        resultType.Fields.Add(fieldResult);
        var pushResultMethod = new MethodDefinition("PushResult", MethodAttributes.Public | MethodAttributes.Static, module.TypeSystem.Void);
        pushResultMethod.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32)); // x
        pushResultMethod.Parameters.Add(new ParameterDefinition(module.TypeSystem.Int32)); // y
        // Bits[x] |= y
        pushResultMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, fieldResult));
        pushResultMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        pushResultMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldelema, module.TypeSystem.Int32));
        pushResultMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Dup));
        pushResultMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldind_I4));
        pushResultMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
        pushResultMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Or));
        pushResultMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Stind_I4));
        pushResultMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        resultType.Methods.Add(pushResultMethod);
        module.Types.Add(resultType);
        var instructionId = 0L;
        foreach (var type in module.Types)
        {
            if (type == resultType) continue;
            Console.WriteLine(type.FullName);
            foreach (var method in type.Methods)
            {
                if (method.IsAbstract) continue;
                InstrumentMethod(method, pushResultMethod, ref instructionId);
            }
        }

        // add constructor that initialize bits
        var methodAttributes = MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
        var constructor = new MethodDefinition(".cctor", methodAttributes, module.TypeSystem.Void);
        var intCount = instructionId / 32 + (instructionId % 32 > 0 ? 1 : 0);
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, (int)intCount));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Newarr, module.TypeSystem.Int32));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Stsfld, fieldResult));
        constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        resultType.Methods.Add(constructor);
        module.Write(@"C:\dev\Compiler\src\artifacts\bin\Draco.Compiler.Tests\debug\Draco.Compiler.Instrumented.dll");
        Console.WriteLine($"Instrumentated {instructionId} instructions");
    }

    private static void InstrumentMethod(MethodDefinition method, MethodDefinition setResult, ref long instructionId)
    {
        var processor = method.Body.GetILProcessor();
        var instructions = processor.Body.Instructions;

        for (var i = 0; i < instructions.Count; i++)
        {
            var instruction = instructions[i];
            if (instruction.OpCode.FlowControl == FlowControl.Next) continue;
            if (instruction.OpCode.FlowControl == FlowControl.Return) continue;
            if (instruction.OpCode.FlowControl == FlowControl.Meta) continue;
            var bitOffset = 1 << (int)(instructionId % 32);
            var intIndex = (int)(instructionId / 32);
            instructionId += 1;
            processor.InsertBefore(instruction, Instruction.Create(OpCodes.Ldc_I4, intIndex));
            processor.InsertBefore(instruction, Instruction.Create(OpCodes.Ldc_I4, bitOffset));
            processor.InsertBefore(instruction, Instruction.Create(OpCodes.Call, setResult));
            i += 3;
        }
    }
}
