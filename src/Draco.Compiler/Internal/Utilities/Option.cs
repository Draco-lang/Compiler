using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Utilities;

/// <summary>
/// A type representing an optional (nullable) value, that works for both reference and value types.
/// </summary>
/// <typeparam name="T">The type of the optional value.</typeparam>
internal readonly struct Option<T> : IEquatable<Option<T>>, IEquatable<T>
{
    /// <summary>
    /// A none value.
    /// </summary>
    public static readonly Option<T> None = new();

    /// <summary>
    /// The unwrapped value, in case it's not none.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public T? Value => this.IsSome
        ? this.value
        : throw new InvalidOperationException("Option was None");

    /// <summary>
    /// True, if the option contains a value.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Value))]
    public bool IsSome { get; }

    /// <summary>
    /// True, if the option is none.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsNone => !this.IsSome;

    private readonly T value;

    public Option()
    {
        this.IsSome = false;
        this.value = default!;
    }

    public Option(T value)
    {
        this.IsSome = true;
        this.value = value;
    }

    public override bool Equals([NotNullWhen(true)] object? obj) =>
           obj is Option<T> other
        && this.Equals(other);
    public bool Equals(Option<T> other)
    {
        if (this.IsNone) return other.IsNone;
        if (other.IsNone) return false;
        return this.Value.Equals(other.Value);
    }
    public bool Equals(T? other) => this.IsSome && this.Value.Equals(other);
    public override int GetHashCode() => this.IsSome
        ? this.Value.GetHashCode()
        : 0;

    /// <summary>
    /// Maps the value of the option to another type.
    /// </summary>
    /// <typeparam name="U">The type of the new value.</typeparam>
    /// <param name="transform">A transformation function mapping the value.</param>
    /// <returns>A new option of either the new transformed value or none.</returns>
    public Option<U> Map<U>(Func<T, U> transform) => this.IsSome
        ? new(transform(this.Value))
        : Option<U>.None;

    /// <summary>
    /// Binds the value to a new option with another value type.
    /// </summary>
    /// <typeparam name="U">The type of the new value.</typeparam>
    /// <param name="transform">A transformation function mapping the value to a new option.</param>
    /// <returns>A new option of either the returned option or none.</returns>
    public Option<U> BindOk<U>(Func<T, Option<U>> transform) => this.IsSome
        ? transform(this.Value)
        : Option<U>.None;

    /// <summary>
    /// Matches the value or none.
    /// </summary>
    /// <typeparam name="TResult">The type of the resulting value.</typeparam>
    /// <param name="ifSome">The function to apply if the result is some.</param>
    /// <param name="ifNone">The function to apply if the result is none.</param>
    /// <returns>The result of matching either the value or none.</returns>
    public TResult Match<TResult>(Func<T, TResult> ifSome, Func<TResult> ifNone) => this.IsSome
        ? ifSome(this.Value)
        : ifNone();

    /// <summary>
    /// Switches on the value or none.
    /// </summary>
    /// <param name="ifSome">The function to apply if there is a value.</param>
    /// <param name="isNone">The function to apply if the value is none.</param>
    public void Switch(Action<T> ifSome, Action isNone)
    {
        if (this.IsSome)
        {
            ifSome(this.Value);
        }
        else
        {
            isNone();
        }
    }

    /// <summary>
    /// Returns the value or a value from a factory function.
    /// </summary>
    /// <param name="factory">The factory function to invoke if the value is none.</param>
    public T ValueOr(Func<T> factory) => this.IsSome
        ? this.Value
        : factory();

    /// <summary>
    /// Returns the value or a default value.
    /// </summary>
    /// <param name="defaultValue">The value to return if the value is none.</param>
    public T ValueOr(T defaultValue) =>
        this.ValueOr(() => defaultValue);

    /// <summary>
    /// Implicitly converts a value to an <see cref="Option{T}"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator Option<T>(T value) =>
        new(value);
}

/// <summary>
/// Extension functionality for <see cref="Option{T}"/>.
/// </summary>
internal static class Option
{
    /// <summary>
    /// Constructs an <see cref="Option{T}"/> containing the given value.
    /// </summary>
    /// <typeparam name="T">The type of the optional value.</typeparam>
    /// <param name="value">The stored value.</param>
    /// <returns>The <paramref name="value"/> wrapped up in <see cref="Option{T}"/>.</returns>
    public static Option<T> Some<T>(T value) => new(value);

    /// <summary>
    /// Constructs a none <see cref="Option{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the optional value.</typeparam>
    /// <returns>An <see cref="Option{T}"/> representing no value.</returns>
    public static Option<T> None<T>() => Option<T>.None;

    /// <summary>
    /// Constructs an <see cref="Option{T}"/> from a nullable reference.
    /// </summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="value">The reference to construct from.</param>
    /// <returns>The optional representation of <paramref name="value"/>.</returns>
    public static Option<T> FromNullableReference<T>(T? value)
        where T : class => value is null
        ? Option<T>.None
        : new(value);

    /// <summary>
    /// Constructs an <see cref="Option{T}"/> from a nullable value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The nullable value to construct from.</param>
    /// <returns>The optional representation of <paramref name="value"/>.</returns>
    public static Option<T> FromNullableValue<T>(T? value)
        where T : struct => value is null
        ? Option<T>.None
        : new(value.Value);

    /// <summary>
    /// Converts an <see cref="Option{T}"/> to a nullable reference.
    /// </summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="option">The <see cref="Option{T}"/> to convert.</param>
    /// <returns>The value of <paramref name="option"/>, or null.</returns>
    public static T? ToNullableReference<T>(this Option<T> option)
        where T : class => option.IsSome
        ? option.Value
        : null;

    /// <summary>
    /// Converts an <see cref="Option{T}"/> to a nullable value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="option">The <see cref="Option{T}"/> to convert.</param>
    /// <returns>The value of <paramref name="option"/>, or null.</returns>
    public static T? ToNullableValue<T>(this Option<T> option)
        where T : struct => option.IsSome
        ? option.Value
        : null;
}
