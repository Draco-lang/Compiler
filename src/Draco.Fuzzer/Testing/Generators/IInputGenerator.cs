namespace Draco.Fuzzer.Testing.Generators;

internal interface IInputGenerator<T>
{
    public T NextExpoch();

    public T NextMutation();
}
