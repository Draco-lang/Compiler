namespace Draco.Compiler.Internal.Syntax.Formatting;

public class Box<T>(T value)
{
    protected T value = value;
    public T Value => this.value;

    public static implicit operator Box<T>(T value) => new(value);
}
