using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using IMoRS.DTOs;
using IMoRS.Services;
using Mapsui;
using Mapsui.Layers;

namespace IMoRS.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    #region Переменные

    [ObservableProperty] private MarkerDto? selectedMarker;
    [ObservableProperty] private Avalonia.Media.Imaging.Bitmap? userImage;
    [ObservableProperty] private Avalonia.Media.Imaging.Bitmap? selectedIcon;
    [ObservableProperty] private SignItem? selectedSign;
    [ObservableProperty] private ObservableCollection<SignItem> _filteredImages = new();

    [ObservableProperty] private double iconsOpacity = 0;
    [ObservableProperty] private double appOpacity = 0;
    [ObservableProperty] private double descOpacity = 0;
    [ObservableProperty] private double editPartsOpacity = 0;
    [ObservableProperty] private double settingsOpacity = 0;
    [ObservableProperty] private double overlayOpacity;
    [ObservableProperty] private double panelWidth1 = 30;
    [ObservableProperty] private double panelWidth2 = 15;
    [ObservableProperty] private double borderListWidth = 25;
    [ObservableProperty] private double borderSettingsWidth = 25;
    [ObservableProperty] private double buttonHeight2 = 0;
    [ObservableProperty] private double borderListHeight = 0;
    [ObservableProperty] private double borderSettingsHeight = 0;
    [ObservableProperty] private double buttonHeight1 = 43;
    [ObservableProperty] private double sliderHeight = 0;
    [ObservableProperty] private double markerScale = 0.2;
    [ObservableProperty] private double oldMarkerScale;

    [ObservableProperty] private bool _isMaximized = false;
    [ObservableProperty] private bool _isAddingMarker = false;
    [ObservableProperty] private bool _isPanel2Open;
    [ObservableProperty] private bool _isListOpen;
    [ObservableProperty] private bool _isMapVisible = false;
    [ObservableProperty] private bool _isEditing = false;
    [ObservableProperty] private bool _isPanel1Open = false;

    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private string? selectedIconPath;
    [ObservableProperty] private string? customMarkerImagePath;
    [ObservableProperty] private string description = string.Empty;

    [ObservableProperty] private Map? _map;

    public ObservableCollection<SignItem> Images { get; set; }

    private readonly MarkerService _markerService = new();
    private readonly List<IFeature> _markers = [];

    private readonly MemoryLayer _markerLayer = new()
    {
        Style = null,
        Name = "Markers",
        Features = new List<IFeature>()
    };

    private double? _pendingMarkerX;
    private double? _pendingMarkerY;

    #endregion

    public MainWindowViewModel()
    {
        Images = new ObservableCollection<SignItem>();

        AppOpacity = 0;
        IconsOpacity = 0;
        CreateMap();
        LoadImages();
        _ = LoadApp();
        InitializeSettings();
    }

    private async System.Threading.Tasks.Task LoadApp()
    {
        await System.Threading.Tasks.Task.Delay(1000);
        AppOpacity = 1;
        await System.Threading.Tasks.Task.Delay(1000);
        IsMapVisible = true;
    }
}