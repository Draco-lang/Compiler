using System;

namespace Draco.Compiler.Internal.Syntax.Formatting;

/// <summary>
/// Represent a value that may be set later.
/// </summary>
/// <typeparam name="T">The type of the value</typeparam>
public class Future<T>
{
    /// <summary>
    /// A future that may be set later.
    /// </summary>
    public Future()
    {
    }

    /// <summary>
    /// An already completed future.
    /// </summary>
    /// <param name="value">The value of the future. </param>
    public Future(T value)
    {
        this.IsCompleted = true;
        this.value = value;
    }

    private T? value;

    /// <summary>
    /// Whether the future is completed or not.
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// The value of the future.
    /// Will throw when accessed if the future is not completed.
    /// </summary>
    public T Value
    {
        get
        {
            if (!this.IsCompleted)
            {
                throw new InvalidOperationException("Cannot access value when not completed.");
            }
            return this.value!;
        }
    }

    /// <summary>
    /// Sets the value of the future.
    /// </summary>
    /// <param name="value"></param>
    public void SetValue(T value)
    {
        this.IsCompleted = true;
        this.value = value;
    }

    /// <summary>
    /// Reset the future to be not completed.
    /// </summary>
    public void Reset()
    {
        this.IsCompleted = false;
        this.value = default;
    }

    public static implicit operator Future<T>(T value) => new(value);
}
