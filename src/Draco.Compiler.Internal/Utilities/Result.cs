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
    /// <summary>
    /// The value of the result. Throws an exception if the result is not an ok value.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public TOk? Ok => this.IsOk
        ? this.ok
        : throw new InvalidOperationException("Result is not an ok value.");

    /// <summary>
    /// The error of the result. Throws an exception if the result is not an error.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public TError? Error => this.IsOk
        ? throw new InvalidOperationException("Result is not an error.")
        : this.error;

    /// <summary>
    /// Whether the result is an ok value or not.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Ok))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsOk { get; }

    private readonly TOk ok;
    private readonly TError error;

    /// <summary>
    /// Initializes a new <see cref="Result{T, TError}"/> with an ok value.
    /// </summary>
    /// <param name="ok">The ok value.</param>
    public Result(TOk ok)
    {
        this.ok = ok;
        this.error = default!;
        this.IsOk = true;
    }

    /// <summary>
    /// Initializes a new <see cref="Result{T, TError}"/> with an error value.
    /// </summary>
    /// <param name="error">The error value.</param>
    public Result(TError error)
    {
        this.ok = default!;
        this.error = error;
        this.IsOk = false;
    }

    /// <summary>
    /// Maps the ok value of the result to another type.
    /// </summary>
    /// <typeparam name="UOk">The type of the new ok value.</typeparam>
    /// <param name="transform">A transformation function mapping the ok value.</param>
    /// <returns>A new result of either the new transformed ok value or the previous error.</returns>
    public Result<UOk, TError> MapOk<UOk>(Func<TOk, UOk> transform) => this.IsOk
        ? new(transform(this.ok))
        : new(this.error);

    /// <summary>
    /// Maps the inner error value of the result to another error type.
    /// </summary>
    /// <typeparam name="UError">The type of the new error value.</typeparam>
    /// <param name="transform">A transformation function mapping the error value.</param>
    /// <returns>A new result of either the new transformed error value or the previous ok value.</returns>
    public Result<TOk, UError> MapError<UError>(Func<TError, UError> transform) => this.IsOk
        ? new(this.ok)
        : new(transform(this.error));

    /// <summary>
    /// Binds the ok value to a new result with another ok value type.
    /// </summary>
    /// <typeparam name="UOk">The type of the new ok value.</typeparam>
    /// <param name="transform">A transformation function mapping the ok value to a new result.</param>
    /// <returns>A new result of either the returned result or the previous error.</returns>
    public Result<UOk, TError> BindOk<UOk>(Func<TOk, Result<UOk, TError>> transform) => this.IsOk
        ? transform(this.ok)
        : new(this.error);

    /// <summary>
    /// Binds the error value to a new result with another error type.
    /// </summary>
    /// <typeparam name="UError">The type of the new error value.</typeparam>
    /// <param name="transform">A transformation function mapping the error value to a new result.</param>
    /// <returns>A new result of either the returned result or the previous ok value.</returns>
    public Result<TOk, UError> BindError<UError>(Func<TError, Result<TOk, UError>> transform) =>
        this.IsOk
            ? new(this.ok)
            : transform(this.error);

    /// <summary>
    /// Matches the ok value or error.
    /// </summary>
    /// <typeparam name="TResult">The type of the resulting value.</typeparam>
    /// <param name="ifOk">The function to apply if the result is an ok value.</param>
    /// <param name="ifError">The function to apply if the result is an error value.</param>
    /// <returns>The result of matching either the ok value or the error value.</returns>
    public TResult Match<TResult>(Func<TOk, TResult> ifOk, Func<TError, TResult> ifError) => this.IsOk
        ? ifOk(this.ok)
        : ifError(this.error);

    /// <summary>
    /// Switches on the ok value or error.
    /// </summary>
    /// <param name="ifOk">The function to apply if the result is an ok value.</param>
    /// <param name="ifError">The function to apply if the result is an error value.</param>
    public void Switch(Action<TOk> ifOk, Action<TError> ifError)
    {
        if (this.IsOk)
        {
            ifOk(this.ok);
        }
        else
        {
            ifError(this.error);
        }
    }

    /// <summary>
    /// Returns the ok value or a value from a factory function.
    /// </summary>
    /// <param name="factory">The factory function to invoke if the result is not an ok value.</param>
    public TOk OkOr(Func<TOk> factory) => this.IsOk
        ? this.ok
        : factory();

    /// <summary>
    /// Returns the ok value or a default value.
    /// </summary>
    /// <param name="defaultValue">The value to return if the result is not an ok value.</param>
    public TOk OkOr(TOk defaultValue) =>
        this.OkOr(() => defaultValue);

    /// <summary>
    /// Returns the error value or a value from a factory function.
    /// </summary>
    /// <param name="factory">The factory function to invoke if the result is not an error value.</param>
    public TError ErrorOr(Func<TError> factory) => this.IsOk
        ? factory()
        : this.error;

    /// <summary>
    /// Returns the error value or a default value.
    /// </summary>
    /// <param name="defaultValue">The value to return if the result is not an error value.</param>
    public TError ErrorOr(TError defaultValue) =>
        this.ErrorOr(() => defaultValue);

    /// <summary>
    /// Implicitly converts an ok value to a <see cref="Result{T, TError}"/>.
    /// </summary>
    /// <param name="ok">The ok value to convert.</param>
    public static implicit operator Result<TOk, TError>(TOk ok) =>
        new(ok);

    /// <summary>
    /// Implicitly converts an error value to a <see cref="Result{T, TError}"/>.
    /// </summary>
    /// <param name="error">The error value to convert.</param>
    public static implicit operator Result<TOk, TError>(TError error) =>
        new(error);
}
