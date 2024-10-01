using System;

namespace Draco.Fuzzing.Components;

/// <summary>
/// Compresses the input data.
/// </summary>
/// <typeparam name="TInput">The type of the input data.</typeparam>
/// <typeparam name="TCompressedInput">The type of the compressed input data.</typeparam>
public interface IInputCompressor<TInput, TCompressedInput>
{
    /// <summary>
    /// Compresses the input data.
    /// </summary>
    /// <param name="input">The input data to compress.</param>
    /// <returns>The compressed input data.</returns>
    public TCompressedInput Compress(TInput input);

    /// <summary>
    /// Decompresses the input data.
    /// </summary>
    /// <param name="compressedInput">The compressed input data to decompress.</param>
    /// <returns>The decompressed input data.</returns>
    public TInput Decompress(TCompressedInput compressedInput);
}

/// <summary>
/// Factory for common input compression logic.
/// </summary>
public static class InputCompressor
{
    /// <summary>
    /// Creates an input compressor that does nothing.
    /// </summary>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <returns>A compressor that does nothing.</returns>
    public static IInputCompressor<TInput, TInput> Null<TInput>() => new NullCompressor<TInput>();

    /// <summary>
    /// Creates an input compressor that converts the input to a string by calling <see cref="object.ToString"/>
    /// and then parses it back using the given parser function.
    /// </summary>
    /// <typeparam name="TInput">The type of the input data.</typeparam>
    /// <param name="parser">The parser function to convert the string back to the input type.</param>
    /// <returns>The input compressor.</returns>
    public static IInputCompressor<TInput, string> String<TInput>(Func<string, TInput> parser) => new StringCompressor<TInput>(parser);

    private sealed class NullCompressor<TInput> : IInputCompressor<TInput, TInput>
    {
        public TInput Compress(TInput input) => input;
        public TInput Decompress(TInput compressedInput) => compressedInput;
    }

    private sealed class StringCompressor<TInput>(Func<string, TInput> parser) : IInputCompressor<TInput, string>
    {
        public string Compress(TInput input) => input?.ToString() ?? string.Empty;
        public TInput Decompress(string compressedInput) => parser(compressedInput);
    }
}
