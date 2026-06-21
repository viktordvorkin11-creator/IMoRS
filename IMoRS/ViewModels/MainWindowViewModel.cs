using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HarfBuzzSharp;
using IMoRS.Models;
using IMoRS.Services;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;

namespace IMoRS.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private Map? _map;

    [ObservableProperty] private bool isMaximized = false;

    [ObservableProperty] private double panelWidth1 = 30;

    [ObservableProperty] private double panelWidth2 = 30;

    [ObservableProperty] private bool isAddingMarker = false;

    [ObservableProperty] private double arrowAngle;

    [ObservableProperty] private bool isPanelOpen;

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

    partial void OnIsPanelOpenChanged(bool value)
    {
        OnPropertyChanged(nameof(ArrowTransform));
    }

    public ObservableCollection<SignInfo> Signs { get; } = new(SignService.Signs);

    private readonly MarkerDbService _database = new();
    private readonly List<IFeature> _markers = [];

    public MainWindowViewModel()
    {
        ArrowAngle = 180;
        CreateMap();
    }

    private readonly MemoryLayer _markerLayer = new();

    private void CreateMap()
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());

        _markerLayer.Name = "Markers";

        foreach (var marker in _database.LoadMarkers())
        {
            var feature = new PointFeature(SphericalMercator.FromLonLat(marker.X, marker.Y));

            feature.Styles.Add(new SymbolStyle());

            _markers.Add(feature);
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

        Map = map;
    }

    public void AddMarker(double x, double y)
    {
        var feature = new PointFeature(SphericalMercator.FromLonLat(x, y).ToMPoint());

        feature.Styles.Add(new SymbolStyle());

        _markers.Add(feature);

        _markerLayer.Features = _markers;

        _database.AddMarker(x, y);

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
        PanelWidth2 = 30;
        ArrowAngle = 180;
        IsPanelOpen = false;
    }

    [RelayCommand]
    public void TogglePanel2()
    {
        IsPanelOpen = !IsPanelOpen;

        if (IsPanelOpen)
        {
            PanelWidth2 = App.MainWindow!.Bounds.Width * 0.3;
            ArrowAngle = 0;
        }
        else
        {
            PanelWidth2 = 30;
            ArrowAngle = 180;
        }
    }
}