namespace Draco.Fuzzer.Testing.Generators;

internal interface IInputGenerator
{
    public string NextExpoch();

    public string NextMutation();
}
