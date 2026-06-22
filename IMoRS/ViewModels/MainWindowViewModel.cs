using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HarfBuzzSharp;
using IMoRS.DTOs;
using IMoRS.Models;
using IMoRS.Services;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using IMoRS.Services;
using Mapsui.UI.Avalonia;

namespace IMoRS.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private Map? _map;

    [ObservableProperty] private bool isMaximized = false;

    [ObservableProperty] private double panelWidth1 = 30;

    [ObservableProperty] private double panelWidth2 = 15;

    [ObservableProperty] private bool isAddingMarker = false;

    [ObservableProperty] private double arrowAngle;

    [ObservableProperty] private bool isPanelOpen;

    [ObservableProperty] private double borderListWidth = 25;

    [ObservableProperty] private double borderListHeight = 0;

    [ObservableProperty] private bool isListOpen;

    [ObservableProperty] private double overlayOpacity;

    [ObservableProperty] private MarkerDto? selectedMarker;

    private readonly MarkerService _markerService = new();
    
    public string ArrowTransform
    {
        get
        {
            if (IsPanelOpen)
                return "rotate(180deg)";
            else
                return "rotate(0deg)";
        }
    }

    private readonly Dictionary<string, int> _bitmapIds = new();

    partial void OnIsPanelOpenChanged(bool value)
    {
        OnPropertyChanged(nameof(ArrowTransform));
    }

    private readonly MarkerService _database = new();
    private readonly List<IFeature> _markers = [];

    public MainWindowViewModel()
    {
        ArrowAngle = 180;
        CreateMap();
    }

    private readonly MemoryLayer _markerLayer = new()
    {
        Style = null,
        Name = "Merkers",
        Features = new List<IFeature>()
    };

    private void CreateMap()
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());

        _markerLayer.Name = "Markers";

        foreach (var marker in _markerService.GetAll())
        {
            if (marker.ImagePath != null)
            {
                var feature = new PointFeature(
                    SphericalMercator.FromLonLat(marker.X, marker.Y));

                feature.Styles.Add(new ImageStyle
                {
                    Image = $"file://{marker.ImagePath}",
                    SymbolScale = 0.5
                });

                _markers.Add(feature);
            }
        }

        _markerLayer.Features = _markers;

        map.Layers.Add(_markerLayer);

        var state = MapStateService.Load();

        if (state != null)
        {
            map.Navigator.CenterOn(state.X, state.Y);
            map.Navigator.ZoomTo(state.Resolution);
        }
        else
        {
            var (centerX, centerY) = SphericalMercator.FromLonLat(82.9204, 55.0302);
            map.Navigator.CenterOn(centerX, centerY);
            map.Navigator.ZoomTo(12);
        }

        Map.Navigator.ViewportChanged += OnViewportChanged;

        Map = map;
    }

    private void OnViewportChanged(object? sender, EventArgs e)
    {
        if (_markerLayer == null) return;

        var resolution = Map.Navigator.Viewport.Resolution;

        double scale;
        double baseSize = 0.5;

        if (resolution > 5000)
        {
            scale = baseSize;
        }
        else if (resolution > 1000)
        {
            double t = (5000 - resolution) / 4000;
            scale = baseSize * (1 - t * 0.7);
        }
        else
        {
            scale = baseSize * 0.3;
        }

        foreach (var feature in _markerLayer.Features)
        {
            var style = feature.Styles.OfType<ImageStyle>().FirstOrDefault();
            if (style != null)
            {
                style.SymbolScale = scale;
            }
        }

        OnPropertyChanged(nameof(Map));
    }


    public void AddMarker(double x, double y)
    {
        var feature = new PointFeature(SphericalMercator.FromLonLat(x, y).ToMPoint());

        feature.Styles.Add(new SymbolStyle());

        _markers.Add(feature);

        _markerLayer.Features = _markers;

        _markerService.Add(x, y);

        Map?.Refresh();

        Map?.Refresh();
    }

    [RelayCommand]
    public void Close()
    {
        App.MainWindow?.Close();
    }

    [RelayCommand]
    public void Minimize()
    {
        App.MainWindow?.WindowState = WindowState.Minimized;
    }


    [RelayCommand]
    public void MaxRestore()
    {
        App.MainWindow?.WindowState = App.MainWindow?.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
        if (App.MainWindow?.WindowState == WindowState.Maximized)
        {
            IsMaximized = true;
        }
        else
        {
            IsMaximized = false;
        }
    }

    [RelayCommand]
    public void OpenPanel1()
    {
        PanelWidth1 = App.MainWindow!.Bounds.Width * 0.25;
    }

    [RelayCommand]
    public void ClosePanel1()
    {
        PanelWidth1 = 30;
    }

    [RelayCommand]
    public void ClosePanel2()
    {
        PanelWidth2 = 15;
        ArrowAngle = 180;
        IsPanelOpen = false;
    }

    [RelayCommand]
    public void OpenPanel2()
    {
        IsPanelOpen = true;

        PanelWidth2 = App.MainWindow!.Bounds.Width * 0.1;
        ArrowAngle = 0;
    }

    [RelayCommand]
    public async Task OpenList()
    {
        IsListOpen = true;
        ClosePanel1();
        ClosePanel2();
        OverlayOpacity = 0.67;
        BorderListHeight = App.MainWindow!.Bounds.Height * 0.7;
        await Task.Delay(200);
        BorderListWidth = App.MainWindow!.Bounds.Width * 0.7;
    }

    [RelayCommand]
    public async Task CloseList()
    {
        IsListOpen = false;
        OverlayOpacity = 0;
        BorderListWidth = 25;
        await Task.Delay(200);
        BorderListHeight = 25;
        await Task.Delay(200);
        BorderListHeight = 0;
        BorderListWidth = App.MainWindow!.Bounds.Width * 0.7;
        await Task.Delay(200);
        BorderListWidth = 25;
    }
}