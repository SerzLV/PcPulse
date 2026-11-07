using System.Windows;
using PcPulse.ViewModels;

namespace PcPulse.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel model)
    {
        InitializeComponent();
        DataContext = model;
        model.CloseRequested += saved => DialogResult = saved;
    }
}
