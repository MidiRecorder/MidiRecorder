using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MidiRecorder.Application;

/// <summary>Represents a min priority queue.</summary>
/// <typeparam name="TElement">Specifies the type of elements in the queue.</typeparam>
/// <typeparam name="TPriority">Specifies the type of priority associated with enqueued elements.</typeparam>
[DebuggerDisplay("Count = {Count}")]
public class RealPriorityQueue<TElement, TPriority>
{
    private readonly PriorityQueue<TElement, (TPriority, long)> _queue;

    /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Generic.PriorityQueue`2" /> class.</summary>
    public RealPriorityQueue()
    {
        _queue = new PriorityQueue<TElement, (TPriority, long)>();
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Generic.PriorityQueue`2" /> class with the specified initial capacity.</summary>
    /// <param name="initialCapacity">Initial capacity to allocate in the underlying heap array.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">The specified <paramref name="initialCapacity" /> was negative.</exception>
    public RealPriorityQueue(int initialCapacity)
    {
        _queue = new PriorityQueue<TElement, (TPriority, long)>(initialCapacity);
    }


    public class TimestampComparer : IComparer<(TPriority, long)>
    {
        private readonly IComparer<TPriority> _comparer;

        public TimestampComparer(IComparer<TPriority> comparer)
        {
            _comparer = comparer;
        }

        public int Compare((TPriority, long) x, (TPriority, long) y)
        {
            var result = _comparer.Compare(x.Item1, y.Item1);
            return result == 0 ? x.Item2.CompareTo(y.Item2) : result;
        }
    }

    public class PriorityComparer : IComparer<TPriority>
    {
        private readonly IComparer<(TPriority, long)> _comparer;

        public PriorityComparer(IComparer<(TPriority, long)> comparer)
        {
            _comparer = comparer;
        }

        public int Compare(TPriority? x, TPriority? y)
        {
            return _comparer.Compare((x, 0), (y, 0));
        }
    }
    /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Generic.PriorityQueue`2" /> class with the specified custom priority comparer.</summary>
    /// <param name="comparer">Custom comparer dictating the ordering of elements.
    /// Uses <see cref="P:System.Collections.Generic.Comparer`1.Default" /> if the argument is <see langword="null" />.</param>
    public RealPriorityQueue(IComparer<TPriority>? comparer)
    {
        _queue = new PriorityQueue<TElement, (TPriority, long)>(comparer == null ? null : new TimestampComparer(comparer));
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Generic.PriorityQueue`2" /> class with the specified initial capacity and custom priority comparer.</summary>
    /// <param name="initialCapacity">Initial capacity to allocate in the underlying heap array.</param>
    /// <param name="comparer">Custom comparer dictating the ordering of elements.
    /// Uses <see cref="P:System.Collections.Generic.Comparer`1.Default" /> if the argument is <see langword="null" />.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">The specified <paramref name="initialCapacity" /> was negative.</exception>
    public RealPriorityQueue(int initialCapacity, IComparer<TPriority>? comparer)
    {
        _queue = new PriorityQueue<TElement, (TPriority, long)>(initialCapacity, comparer == null ? null : new TimestampComparer(comparer));
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Generic.PriorityQueue`2" /> class that is populated with the specified elements and priorities.</summary>
    /// <param name="items">The pairs of elements and priorities with which to populate the queue.</param>
    /// <exception cref="T:System.ArgumentNullException">The specified <paramref name="items" /> argument was <see langword="null" />.</exception>
    public RealPriorityQueue(
        IEnumerable<(TElement, TPriority)> items)
    {
        _queue = new PriorityQueue<TElement, (TPriority, long)>(items.Select((x, i) => (x.Item1, (x.Item2, (long)i))));
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Collections.Generic.PriorityQueue`2" /> class that is populated with the specified elements and priorities, and with the specified custom priority comparer.</summary>
    /// <param name="items">The pairs of elements and priorities with which to populate the queue.</param>
    /// <param name="comparer">Custom comparer dictating the ordering of elements.
    /// Uses <see cref="P:System.Collections.Generic.Comparer`1.Default" /> if the argument is <see langword="null" />.</param>
    /// <exception cref="T:System.ArgumentNullException">The specified <paramref name="items" /> argument was <see langword="null" />.</exception>
    public RealPriorityQueue(
        IEnumerable<(TElement Element, TPriority Priority)> items,
        IComparer<TPriority>? comparer)
    {
        _queue = new PriorityQueue<TElement, (TPriority, long)>(
            items.Select((x, i) => (x.Item1, (x.Item2, (long)i))),
            comparer == null ? null : new TimestampComparer(comparer));
    }

    /// <summary>Gets the number of elements contained in the <see cref="T:System.Collections.Generic.PriorityQueue`2" />.</summary>
    public int Count => _queue.Count;

    /// <summary>Gets the priority comparer used by the <see cref="T:System.Collections.Generic.PriorityQueue`2" />.</summary>
    public IComparer<TPriority> Comparer => new PriorityComparer(_queue.Comparer);

    /// <summary>Adds the specified element with associated priority to the <see cref="T:System.Collections.Generic.PriorityQueue`2" />.</summary>
    /// <param name="element">The element to add to the <see cref="T:System.Collections.Generic.PriorityQueue`2" />.</param>
    /// <param name="priority">The priority with which to associate the new element.</param>
    public void Enqueue(TElement element, TPriority priority)
        => _queue.Enqueue(element, (priority, Stopwatch.GetTimestamp()));

    /// <summary>Returns the minimal element from the <see cref="T:System.Collections.Generic.PriorityQueue`2" /> without removing it.</summary>
    /// <exception cref="T:System.InvalidOperationException">The <see cref="T:System.Collections.Generic.PriorityQueue`2" /> is empty.</exception>
    /// <returns>The minimal element of the <see cref="T:System.Collections.Generic.PriorityQueue`2" />.</returns>
    public TElement Peek() => _queue.Peek();

    /// <summary>Removes and returns the minimal element from the <see cref="T:System.Collections.Generic.PriorityQueue`2" />.</summary>
    /// <exception cref="T:System.InvalidOperationException">The queue is empty.</exception>
    /// <returns>The minimal element of the <see cref="T:System.Collections.Generic.PriorityQueue`2" />.</returns>
    public TElement Dequeue() => _queue.Dequeue();

    /// <summary>Removes the minimal element from the <see cref="T:System.Collections.Generic.PriorityQueue`2" />, and copies it to the <paramref name="element" /> parameter, and its associated priority to the <paramref name="priority" /> parameter.</summary>
    /// <param name="element">The removed element.</param>
    /// <param name="priority">The priority associated with the removed element.</param>
    /// <returns>
    /// <see langword="true" /> if the element is successfully removed; <see langword="false" /> if the <see cref="T:System.Collections.Generic.PriorityQueue`2" /> is empty.</returns>
    public bool TryDequeue([MaybeNullWhen(false)] out TElement element, [MaybeNullWhen(false)] out TPriority priority)
    {
        var result = _queue.TryDequeue(out element, out var priority2);
        priority = priority2.Item1;
        return result;
    }

    /// <summary>Returns a value that indicates whether there is a minimal element in the <see cref="T:System.Collections.Generic.PriorityQueue`2" />, and if one is present, copies it to the <paramref name="element" /> parameter, and its associated priority to the <paramref name="priority" /> parameter.
    /// The element is not removed from the <see cref="T:System.Collections.Generic.PriorityQueue`2" />.</summary>
    /// <param name="element">The minimal element in the queue.</param>
    /// <param name="priority">The priority associated with the minimal element.</param>
    /// <returns>
    /// <see langword="true" /> if there is a minimal element; <see langword="false" /> if the <see cref="T:System.Collections.Generic.PriorityQueue`2" /> is empty.</returns>
    public bool TryPeek([MaybeNullWhen(false)] out TElement element, [MaybeNullWhen(false)] out TPriority priority)
    {
        var result = _queue.TryPeek(out element, out var priority2);
        priority = priority2.Item1;
        return result;
    }

    /// <summary>Adds the specified element with associated priority to the <see cref="T:System.Collections.Generic.PriorityQueue`2" />, and immediately removes the minimal element, returning the result.</summary>
    /// <param name="element">The element to add to the <see cref="T:System.Collections.Generic.PriorityQueue`2" />.</param>
    /// <param name="priority">The priority with which to associate the new element.</param>
    /// <returns>The minimal element removed after the enqueue operation.</returns>
    public TElement EnqueueDequeue(TElement element, TPriority priority)
    {
        return _queue.EnqueueDequeue(element, (priority, Stopwatch.GetTimestamp()));
    }

    /// <summary>Enqueues a sequence of element/priority pairs to the <see cref="T:System.Collections.Generic.PriorityQueue`2" />.</summary>
    /// <param name="items">The pairs of elements and priorities to add to the queue.</param>
    /// <exception cref="T:System.ArgumentNullException">The specified <paramref name="items" /> argument was <see langword="null" />.</exception>
    public void EnqueueRange(
        IEnumerable<(TElement Element, TPriority Priority)> items)
    {
        _queue.EnqueueRange(items.Select(x => (x.Element, (x.Priority, Stopwatch.GetTimestamp()))));
    }

    /// <summary>Enqueues a sequence of elements pairs to the <see cref="T:System.Collections.Generic.PriorityQueue`2" />, all associated with the specified priority.</summary>
    /// <param name="elements">The elements to add to the queue.</param>
    /// <param name="priority">The priority to associate with the new elements.</param>
    /// <exception cref="T:System.ArgumentNullException">The specified <paramref name="elements" /> argument was <see langword="null" />.</exception>
    public void EnqueueRange(IEnumerable<TElement> elements, TPriority priority)
    {
        _queue.EnqueueRange(elements, (priority, Stopwatch.GetTimestamp()));
    }

    /// <summary>Removes all items from the <see cref="T:System.Collections.Generic.PriorityQueue`2" />.</summary>
    public void Clear()
    {
        _queue.Clear();
    }

    /// <summary>Ensures that the <see cref="T:System.Collections.Generic.PriorityQueue`2" /> can hold up to <paramref name="capacity" /> items without further expansion of its backing storage.</summary>
    /// <param name="capacity">The minimum capacity to be used.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">The specified <paramref name="capacity" /> is negative.</exception>
    /// <returns>The current capacity of the <see cref="T:System.Collections.Generic.PriorityQueue`2" />.</returns>
    public int EnsureCapacity(int capacity)
    {
        return _queue.EnsureCapacity(capacity);
    }

    /// <summary>Sets the capacity to the actual number of items in the <see cref="T:System.Collections.Generic.PriorityQueue`2" />, if that is less than 90 percent of current capacity.</summary>
    public void TrimExcess()
    {
        _queue.TrimExcess();
    }
}
