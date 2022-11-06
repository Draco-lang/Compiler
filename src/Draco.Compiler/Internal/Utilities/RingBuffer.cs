using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Utilities;

/// <summary>
/// A collection that supports fast inserting and removal at both ends.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
internal sealed class RingBuffer<T> : IReadOnlyCollection<T>
{
    private const int DefaultCapacity = 8;

    /// <inheritdoc/>
    public int Count { get; private set; }

    /// <summary>
    /// Gets or sets the capacity of the backing buffer.
    /// </summary>
    public int Capacity
    {
        get => this.storage.Length;
        set
        {
            // If we try to reduce it below the current count, throw
            if (value < this.Count) throw new ArgumentOutOfRangeException(nameof(value));

            // If capacity is already enough, early-return
            if (value <= this.Capacity) return;

            // If we reallocate, we use this occasion to reorder the elements to have the head in the front
            var newStorage = new T[value];
            var (first, second) = this.AsMemory();
            first.CopyTo(newStorage);
            second.CopyTo(newStorage.AsMemory(first.Length));
            this.Head = 0;
            this.storage = newStorage;
        }
    }

    /// <summary>
    /// The index of the first element.
    /// </summary>
    public int Head { get; private set; }

    /// <summary>
    /// The index after the last element.
    /// </summary>
    public int Tail => (this.Head + this.Count) % this.Capacity;

    /// <summary>
    /// Retrieves the element at <paramref name="index"/>, starting counting from the <see cref="Head"/>.
    /// </summary>
    /// <param name="index">The position of the element to retrieve.</param>
    /// <returns>The element <paramref name="index"/> ahead from <see cref="Head"/>.</returns>
    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= this.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return this.storage[(this.Head + index) % this.Capacity];
        }
        set
        {
            if (index < 0 || index >= this.Count) throw new ArgumentOutOfRangeException(nameof(index));
            this.storage[(this.Head + index) % this.Capacity] = value;
        }
    }

    private T[] storage = Array.Empty<T>();

    /// <summary>
    /// Initializes a new, empty <see cref="RingBuffer{T}"/>.
    /// </summary>
    /// <param name="capacity">The default capacity for the backing buffer.</param>
    public RingBuffer(int capacity = DefaultCapacity)
    {
        this.Capacity = capacity;
    }

    /// <summary>
    /// Removes all elements from this <see cref="RingBuffer{T}"/>.
    /// </summary>
    public void Clear()
    {
        var (first, second) = this.AsMemory();
        first.Span.Clear();
        second.Span.Clear();
        this.Head = 0;
        this.Count = 0;
    }

    /// <summary>
    /// Adds <paramref name="item"/> to the front of this <see cref="RingBuffer{T}"/>.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void AddFront(T item)
    {
        this.EnsureCapacity(this.Count + 1);
        this.Head = this.Head == 0 ? this.Capacity - 1 : this.Head - 1;
        this.storage[this.Head] = item;
        ++this.Count;
    }

    /// <summary>
    /// Adds <paramref name="item"/> to the back of this <see cref="RingBuffer{T}"/>.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void AddBack(T item)
    {
        this.EnsureCapacity(this.Count + 1);
        this.storage[this.Tail] = item;
        ++this.Count;
    }

    /// <summary>
    /// Removes an item from the front of this <see cref="RingBuffer{T}"/>.
    /// </summary>
    /// <returns>The removed item.</returns>
    public T RemoveFront()
    {
        if (this.Count == 0) throw new InvalidOperationException("the ring buffer was empty");

        var result = this.storage[this.Head];
        this.storage[this.Head] = default!;
        this.Head = (this.Head + 1) % this.Capacity;
        --this.Count;
        return result;
    }

    /// <summary>
    /// Removes an item from the back of this <see cref="RingBuffer{T}"/>.
    /// </summary>
    /// <returns>The removed item.</returns>
    public T RemoveBack()
    {
        if (this.Count == 0) throw new InvalidOperationException("the ring buffer was empty");

        var index = this.Tail - 1;
        var result = this.storage[index];
        this.storage[index] = default!;
        --this.Count;
        return result;
    }

    /// <summary>
    /// Checks if <paramref name="item"/> is an element of this <see cref="RingBuffer{T}"/>.
    /// </summary>
    /// <param name="item">The item to search for.</param>
    /// <returns>True, if <paramref name="item"/> is an element of this <see cref="RingBuffer{T}"/>.</returns>
    public bool Contains(T item) => this.IndexOf(item) != -1;

    /// <summary>
    /// Searches the index of <paramref name="item"/> in this <see cref="RingBuffer{T}"/>.
    /// </summary>
    /// <param name="item">The item to search for.</param>
    /// <returns>The index of <paramref name="item"/>, or 0 if not found.</returns>
    public int IndexOf(T item)
    {
        var (first, second) = this.AsMemory();
        var index = 0;
        for (var i = 0; i < first.Length; ++i, ++index)
        {
            if (EqualityComparer<T>.Default.Equals(item, first.Span[i])) return index;
        }
        for (var i = 0; i < second.Length; ++i, ++index)
        {
            if (EqualityComparer<T>.Default.Equals(item, second.Span[i])) return index;
        }
        return -1;
    }

    /// <summary>
    /// Copies the <see cref="RingBuffer{T}"/> elements to an existing array, starting at the specified array index.
    /// </summary>
    /// <param name="array">The destination array to copy the elements to.</param>
    /// <param name="arrayIndex">The index in <paramref name="array"/> where the copying starts.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        var (first, second) = this.AsMemory();
        first.CopyTo(array.AsMemory(arrayIndex));
        second.CopyTo(array.AsMemory(arrayIndex + first.Length));
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        var (first, second) = this.AsMemory();
        for (var i = 0; i < first.Length; ++i) yield return first.Span[i];
        for (var i = 0; i < second.Length; ++i) yield return second.Span[i];
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    /// <summary>
    /// Ensures that the capacity of this <see cref="RingBuffer{T}"/> is at least the specified
    /// <paramref name="capacity"/>.
    /// </summary>
    /// <param name="capacity">The minimum capacity to ensure.</param>
    /// <returns>The new capacity of this ring buffer.</returns>
    public int EnsureCapacity(int capacity)
    {
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (this.Capacity < capacity)
        {
            var newCapacity = this.Capacity == 0 ? DefaultCapacity : this.Capacity * 2;
            newCapacity = Math.Max(newCapacity, capacity);
            this.Capacity = newCapacity;
        }
        return this.Capacity;
    }

    /// <summary>
    /// Retrieves the entire allocated storage as two sequential chunks.
    /// </summary>
    /// <returns>A pair of the first and second memory chunk.</returns>
    private (Memory<T> First, Memory<T> Second) AsMemory()
    {
        // For empty buffer, just return an empty pair
        if (this.Count == 0) return (Memory<T>.Empty, Memory<T>.Empty);

        // For non-empty buffers we can assum that if tail <= head, then the buffer is split in 2
        return this.Tail > this.Head
            ? (this.storage.AsMemory(this.Head, this.Count), Memory<T>.Empty)
            : (this.storage.AsMemory(this.Head, this.Capacity - this.Head), this.storage.AsMemory(0, this.Tail));
    }
}
