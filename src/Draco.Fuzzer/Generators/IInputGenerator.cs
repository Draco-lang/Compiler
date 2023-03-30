namespace Draco.Fuzzer.Generators;

/// <summary>
/// Generates input for the fuzzer components.
/// </summary>
/// <typeparam name="T">The type of input the generator generates.</typeparam>
internal interface IInputGenerator<T>
{
    /// <summary>
    /// Generates input for the next epoch of fuzzing.
    /// </summary>
    /// <returns>The new input for the components.</returns>
    public T NextExpoch();

    /// <summary>
    /// Generates input for the next iteration of fuzzing, which is a slight mutation from the last one.
    /// </summary>
    /// <returns>The mutated input from last iteration.</returns>
    public T NextMutation();
}
