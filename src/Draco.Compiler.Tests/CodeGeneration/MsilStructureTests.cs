using System.Reflection;
using Draco.Compiler.Internal;

namespace Draco.Compiler.Tests.CodeGeneration;

public sealed class MsilStructureTests
{
    private readonly struct RelayDisposable(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }

    private const BindingFlags AllBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    private string? CurrentNamespace => this.namespaceStack.Count == 0
        ? null
        : string.Join(".", this.namespaceStack.Reverse());
    private Type CurrentType => this.typeStack.Peek();

    private Assembly assembly = null!;
    private readonly Stack<Type> typeStack = new();
    private readonly Stack<string> namespaceStack = new();

    private void Compile(string source)
    {
        this.assembly = TestUtilities.CompileToAssembly(source);
    }

    private RelayDisposable Ns(string name)
    {
        this.namespaceStack.Push(name);
        return new RelayDisposable(() => this.namespaceStack.Pop());
    }

    private RelayDisposable C(string name, Func<Type, bool> predicate)
    {
        var type = this.typeStack.Count == 0
            // Look up in root assembly
            ? this.assembly.GetType(name)
            // Look up in current type
            : this.CurrentType.GetNestedType(name, AllBindingFlags);

        Assert.NotNull(type);
        Assert.Equal(this.CurrentNamespace, type.Namespace);
        Assert.True(predicate(type));
        this.typeStack.Push(type);

        return new RelayDisposable(() => this.typeStack.Pop());
    }

    private RelayDisposable StaticC(string name, Func<Type, bool> predicate) =>
        this.C(name, t => t.IsAbstract && t.IsSealed && predicate(t));

    private void M(string name, Func<MethodInfo, bool> predicate)
    {
        var method = this.CurrentType.GetMethod(name, AllBindingFlags);
        Assert.NotNull(method);
        Assert.True(predicate(method));
    }

    [Fact]
    public void HelloWorld()
    {
        this.Compile("""
            import System.Console;

            func main() {
                WriteLine("Hello, World!");
            }
            """);

        using (this.StaticC(CompilerConstants.DefaultModuleName, t => !t.IsPublic))
        {
            this.M("main", m => !m.IsPublic && m.IsStatic);
        }
    }
}
