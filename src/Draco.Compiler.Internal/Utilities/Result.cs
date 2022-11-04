using System;
using System.Diagnostics.CodeAnalysis;

namespace Draco.Compiler.Internal.Utilities;

/// <summary>
/// A union type containing either the result of an operation or an error.
/// </summary>
/// <typeparam name="TOk">The type of the inner ok value.</typeparam>
/// <typeparam name="TError">The type of the inner error.</typeparam>
public readonly struct Result<TOk, TError>
{
    private readonly TOk value;
    private readonly TError error;
    private readonly bool isSuccess;

    /// <summary>
    /// The value of the result. Throws an exception if the result is not an ok value.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public TOk? Value => this.isSuccess
        ? this.value
        : throw new InvalidOperationException("Result does not contain a value.");

    /// <summary>
    /// The error of the result. Throws an exception if the result is not an error.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public TError? Error => this.isSuccess
        ? throw new InvalidOperationException("Result does not contain an error.")
        : this.error;

    /// <summary>
    /// Whether the result is an ok value or not.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess =>
        this.isSuccess;

    /// <summary>
    /// Initializes a new <see cref="Result{T, TError}"/> with an ok value.
    /// </summary>
    /// <param name="value">The ok value.</param>
    public Result(TOk value)
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
    /// Maps the ok value of the result to another type.
    /// </summary>
    /// <typeparam name="UOk">The type of the new ok value.</typeparam>
    /// <param name="transform">A transformation function mapping the ok value.</param>
    /// <returns>A new result of either the new transformed ok value or the previous error.</returns>
    public Result<UOk, TError> MapValue<UOk>(Func<TOk, UOk> transform) => this.isSuccess
        ? new(transform(this.value))
        : new(this.error);

    /// <summary>
    /// Maps the inner error value of the result to another error type.
    /// </summary>
    /// <typeparam name="UError">The type of the new error value.</typeparam>
    /// <param name="transform">A transformation function mapping the error value.</param>
    /// <returns>A new result of either the new transformed error value or the previous ok value.</returns>
    public Result<TOk, UError> MapError<UError>(Func<TError, UError> transform) => this.isSuccess
        ? new(this.value)
        : new(transform(this.error));

    /// <summary>
    /// Binds the ok value to a new result with another ok value type.
    /// </summary>
    /// <typeparam name="UOk">The type of the new ok value.</typeparam>
    /// <param name="transform">A transformation function mapping the ok value to a new result.</param>
    /// <returns>A new result of either the returned result or the previous error.</returns>
    public Result<UOk, TError> BindValue<UOk>(Func<TOk, Result<UOk, TError>> transform) => this.isSuccess
        ? transform(this.value)
        : new(this.error);

    /// <summary>
    /// Binds the error value to a new result with another error type.
    /// </summary>
    /// <typeparam name="UError">The type of the new error value.</typeparam>
    /// <param name="transform">A transformation function mapping the error value to a new result.</param>
    /// <returns>A new result of either the returned result or the previous ok value.</returns>
    public Result<TOk, UError> BindError<UError>(Func<TError, Result<TOk, UError>> transform) =>
        this.isSuccess
            ? new(this.value)
            : transform(this.error);

    /// <summary>
    /// Matches the ok value or error.
    /// </summary>
    /// <typeparam name="TResult">The type of the resulting value.</typeparam>
    /// <param name="ifValue">The function to apply if the result is an ok value.</param>
    /// <param name="ifError">The function to apply if the result is an error value.</param>
    /// <returns>The result of matching either the ok value or the error value.</returns>
    public TResult Match<TResult>(Func<TOk, TResult> ifValue, Func<TError, TResult> ifError) => this.isSuccess
        ? ifValue(this.value)
        : ifError(this.error);

    /// <summary>
    /// Switches on the ok value or error.
    /// </summary>
    /// <param name="ifValue">The function to apply if the result is an ok value.</param>
    /// <param name="ifError">The function to apply if the result is an error value.</param>
    public void Switch(Action<TOk> ifValue, Action<TError> ifError)
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
    /// Implicitly converts an ok value to a <see cref="Result{T, TError}"/>.
    /// </summary>
    /// <param name="value">The ok value to convert.</param>
    public static implicit operator Result<TOk, TError>(TOk value) =>
        new(value);

    /// <summary>
    /// Implicitly converts an error value to a <see cref="Result{T, TError}"/>.
    /// </summary>
    /// <param name="error">The error value to convert.</param>
    public static implicit operator Result<TOk, TError>(TError error) =>
        new(error);
}
