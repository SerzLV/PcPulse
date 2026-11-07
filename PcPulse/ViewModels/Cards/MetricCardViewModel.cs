using System.Windows.Media;
using PcPulse.Core.Models;
using PcPulse.Presentation;

namespace PcPulse.ViewModels.Cards;

/// <summary>
/// A dashboard card: a ring gauge with a big number, a list of stats, a hint line and a 60-second history.
/// Subclasses decide what goes where; adding a card means adding a subclass, nothing else changes.
/// </summary>
public abstract class MetricCardViewModel : ObservableObject
{
    private readonly List<StatViewModel> _stats = new();
    private string _subtitle = "";
    private string _mainText = "—";
    private string _caption = "";
    private double _gaugeValue;
    private double _gaugeMaximum = 100;
    private Color _gaugeColor = Palette.Muted;
    private string? _hint;

    protected MetricCardViewModel(string title, Color accent, string historyLabel, double historyMinScale)
    {
        Title = title;
        Accent = accent;
        HistoryLabel = historyLabel;
        History = new HistorySeries(capacity: 60, historyMinScale);
    }

    public string Title { get; }
    public Color Accent { get; }
    public string HistoryLabel { get; }
    public HistorySeries History { get; }
    public IReadOnlyList<StatViewModel> Stats => _stats;

    public string Subtitle { get => _subtitle; protected set => Set(ref _subtitle, value); }
    public string MainText { get => _mainText; private set => Set(ref _mainText, value); }
    public string Caption { get => _caption; private set => Set(ref _caption, value); }
    public double GaugeValue { get => _gaugeValue; private set => Set(ref _gaugeValue, value); }
    public double GaugeMaximum { get => _gaugeMaximum; private set => Set(ref _gaugeMaximum, value); }
    public Color GaugeColor { get => _gaugeColor; private set => Set(ref _gaugeColor, value); }

    public string? Hint
    {
        get => _hint;
        protected set
        {
            if (Set(ref _hint, value)) OnPropertyChanged(nameof(HasHint));
        }
    }

    public bool HasHint => !string.IsNullOrEmpty(_hint);

    /// <summary>Cheap bookkeeping on every poll, even while the window is hidden (e.g. history).</summary>
    public abstract void Record(MonitorSample sample);

    /// <summary>Updates everything shown on the card; called only while the window is visible.</summary>
    public abstract void Render(MonitorSample sample);

    protected StatViewModel AddStat(string label)
    {
        var stat = new StatViewModel(label);
        _stats.Add(stat);
        return stat;
    }

    /// <summary>The big number in the ring. A null gauge value empties the ring.</summary>
    protected void SetMain(string text, string caption, double? gaugeValue, double gaugeMaximum, Color color)
    {
        MainText = text;
        Caption = caption;
        GaugeMaximum = gaugeMaximum;
        GaugeColor = color;
        GaugeValue = gaugeValue ?? 0;
    }
}
