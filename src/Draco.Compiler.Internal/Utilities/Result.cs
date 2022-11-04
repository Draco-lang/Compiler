using System;
using System.Diagnostics.CodeAnalysis;

namespace Draco.Compiler.Internal.Utilities;

/// <summary>
/// A union type containing either the result of an operation or an error.
/// </summary>
/// <typeparam name="T">The type of the inner value.</typeparam>
/// <typeparam name="TError">The type of the inner error.</typeparam>
public readonly struct Result<T, TError>
{
    private readonly T value;
    private readonly TError error;
    private readonly bool isSuccess;

    /// <summary>
    /// The value of the result. Throws an exception if the result is not a value.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public T? Value =>
        this.isSuccess
            ? this.value
            : throw new InvalidOperationException("Result does not contain a value.");

    /// <summary>
    /// The error of the result. Throws an exception if the result is not an error.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public TError? Error =>
        this.isSuccess
            ? throw new InvalidOperationException("Result does not contain an error.")
            : this.error;

    /// <summary>
    /// Whether the result is a successful value or not.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess =>
        this.isSuccess;



    /// <summary>
    /// Initializes a new <see cref="Result{T, TError}"/> with a successful value.
    /// </summary>
    /// <param name="value">The success value.</param>
    public Result(T value)
    {
        this.value = value;
        this.error = default!;
        this.isSuccess = true;
    }

    /// <summary>
    /// Initializes a new <see cref="Result{T, TError}"/> with an error value.
    /// </summary>
    /// <param name="error">The error value.</param>
    public Result(TError error)
    {
        this.value = default!;
        this.error = error;
        this.isSuccess = false;
    }



    /// <summary>
    /// Maps the inner value of the result to another type.
    /// </summary>
    /// <typeparam name="U">The type of the new inner value.</typeparam>
    /// <param name="transform">A transformation function mapping the inner value.</param>
    /// <returns>A new result of either the transformed value or the previous error.</returns>
    public Result<U, TError> MapValue<U>(Func<T, U> transform) =>
        this.isSuccess
            ? new(transform(this.value))
            : new(this.error);

    /// <summary>
    /// Maps the inner error value of the result to another error type.
    /// </summary>
    /// <typeparam name="UError">The type of the new inner error value.</typeparam>
    /// <param name="transform">A transformation function mapping the inner error value.</param>
    /// <returns>A new result of either the transformed error value or the previous successful value.</returns>
    public Result<T, UError> MapError<UError>(Func<TError, UError> transform) =>
        this.isSuccess
            ? new(this.value)
            : new(transform(this.error));

    /// <summary>
    /// Binds the inner value to a new result with another inner value type.
    /// </summary>
    /// <typeparam name="U">The type of the new inner value.</typeparam>
    /// <param name="transform">A transformation function mapping the inner value to a new result.</param>
    /// <returns>A new result of either the returned result or the previous error.</returns>
    public Result<U, TError> BindValue<U>(Func<T, Result<U, TError>> transform) =>
        this.isSuccess
            ? transform(this.value)
            : new(this.error);

    /// <summary>
    /// Binds the inner error value to a new result with another inner error type.
    /// </summary>
    /// <typeparam name="UError">The type of the new inner error value.</typeparam>
    /// <param name="transform">A transformation function mapping the inner error value to a new result.</param>
    /// <returns>A new result of either the returned result or the previous successful value.</returns>
    public Result<T, UError> BindError<UError>(Func<TError, Result<T, UError>> transform) =>
        this.isSuccess
            ? new(this.value)
            : transform(this.error);

    /// <summary>
    /// Matches the inner value or error.
    /// </summary>
    /// <typeparam name="TResult">The type of the resulting value.</typeparam>
    /// <param name="ifValue">The function to apply if the result is a successful value.</param>
    /// <param name="ifError">The function to apply if the result is an error value.</param>
    /// <returns>The result of matching either the successful value or the error value.</returns>
    public TResult Match<TResult>(Func<T, TResult> ifValue, Func<TError, TResult> ifError) =>
        this.isSuccess
            ? ifValue(this.value)
            : ifError(this.error);

    /// <summary>
    /// Switches on the inner value or error.
    /// </summary>
    /// <param name="ifValue">The function to apply if the result is a successful value.</param>
    /// <param name="ifError">The function to apply if the result is an error value.</param>
    public void Switch(Action<T> ifValue, Action<TError> ifError)
    {
        if (this.isSuccess)
        {
            ifValue(this.value);
        }
        else
        {
            ifError(this.error);
        }
    }



    /// <summary>
    /// Implicitly converts a successful <see cref="T"/> value to a <see cref="Result{T, TError}"/>.
    /// </summary>
    /// <param name="value">The successful value to convert.</param>
    public static implicit operator Result<T, TError>(T value) =>
        new(value);

    /// <summary>
    /// Implicitly converts an error <see cref="TError"/> value to a <see cref="Result{T, TError}"/>.
    /// </summary>
    /// <param name="error">The error value to convert.</param>
    public static implicit operator Result<T, TError>(TError error) =>
        new(error);
}
