namespace PcPulse.ViewModels;

/// <summary>One "label ..... value" row on a card.</summary>
public sealed class StatViewModel : ObservableObject
{
    private string _value = "—";

    public StatViewModel(string label) => Label = label;

    public string Label { get; }

    public string Value
    {
        get => _value;
        set => Set(ref _value, value);
    }
}
