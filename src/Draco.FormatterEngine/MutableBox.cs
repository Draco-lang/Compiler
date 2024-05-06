namespace Draco.Compiler.Internal.Syntax.Formatting;

public sealed class MutableBox<T>(T value) : Box<T>(value)
{
    public new T Value
    {
        get => base.Value;
        set => this.value = value;
    }
}
