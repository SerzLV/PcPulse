using System.Collections;
using System.Collections.Specialized;

namespace PcPulse.ViewModels;

/// <summary>Rolling buffer of the most recent samples for a sparkline. Raises a Reset after every change.</summary>
public sealed class HistorySeries : IReadOnlyList<double>, INotifyCollectionChanged
{
    private static readonly NotifyCollectionChangedEventArgs Reset = new(NotifyCollectionChangedAction.Reset);

    private readonly List<double> _values = new();

    public HistorySeries(int capacity, double minScale)
    {
        Capacity = capacity;
        MinScale = minScale;
    }

    public int Capacity { get; }

    /// <summary>The chart's Y axis never shrinks below this, so a flat idle line stays low.</summary>
    public double MinScale { get; }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int Count => _values.Count;

    public double this[int index] => _values[index];

    public void Push(double value)
    {
        _values.Add(value);
        if (_values.Count > Capacity) _values.RemoveAt(0);
        CollectionChanged?.Invoke(this, Reset);
    }

    public void Clear()
    {
        _values.Clear();
        CollectionChanged?.Invoke(this, Reset);
    }

    public IEnumerator<double> GetEnumerator() => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
