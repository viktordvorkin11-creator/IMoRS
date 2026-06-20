using System;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HarfBuzzSharp;
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

    [ObservableProperty] private double panelWidth = 30;

    [ObservableProperty] private bool isAddingMarker = false;

    public MainWindowViewModel()
    {
        CreateMap();
    }

    private readonly MemoryLayer _markerLayer = new();

    private void CreateMap()
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());

        _markerLayer.Name = "Markers";
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

    public void AddMarker(double lon, double lat)
    {
        var feature = new PointFeature(SphericalMercator.FromLonLat(lon, lat).ToMPoint());

        feature.Styles.Add(new SymbolStyle());

        _markerLayer.Features = [feature];

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
    public void OpenPanel()
    {
        PanelWidth = App.MainWindow!.Bounds.Width * 0.25;
    }

    [RelayCommand]
    public void ClosePanel()
    {
        PanelWidth = 30;
    }
}