using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class SignItem : ObservableObject
{
    [ObservableProperty] private Bitmap? image;
    
    [ObservableProperty] private string? path;
    
    [ObservableProperty] private bool _isSelected;
}