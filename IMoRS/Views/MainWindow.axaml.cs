using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using IMoRS.DTOs;
using IMoRS.Models;
using IMoRS.Services;
using IMoRS.ViewModels;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Manipulations;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.UI.Avalonia;

namespace IMoRS.Views;

public partial class MainWindow : Window
{
    private Point _pointerDownPosition;
    private bool _isPressed;
    private bool _isDragging;
    private bool _isMarkerClicked;

    public MainWindow()
    {
        InitializeComponent();

        mapControl.Info += MapControlInfo;
        mapControl.PointerPressed += OnMapPointerPressed;
        mapControl.PointerMoved += OnMapPointerMoved;
        mapControl.PointerReleased += OnMapPointerReleased;
    }

    private void OnMapPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _pointerDownPosition = e.GetPosition(mapControl);
        _isDragging = false;
        _isPressed = true;
    }

    private void OnMapPointerMoved(object? sender, PointerEventArgs e)
    { 
        if (_isPressed && DataContext is MainWindowViewModel vm)
        {
            var currentPosition = e.GetPosition(mapControl);
            var delta = _pointerDownPosition - currentPosition;
        
            if (Math.Abs(delta.X) > 5 || Math.Abs(delta.Y) > 5)
            {
                _isDragging = true;
                vm.ClosePanel2Command.Execute(null);
                vm.ClosePanel1Command.Execute(null);
            }
        }
    }

    private void OnMapPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isMarkerClicked)
        {
            _isMarkerClicked = false;
            _isPressed = false;
            return;
        }

        if (!_isDragging && DataContext is MainWindowViewModel vm)
        {
            vm.ClosePanel1Command.Execute(null);
            vm.OpenPanel2Command.Execute(null);
        }
    
        _isPressed = false;
        _isDragging = false;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (mapControl.Map is not null)
        {
            var viewport = mapControl.Map.Navigator.Viewport;

            MapStateService.Save(new MapState
            {
                X = viewport.CenterX,
                Y = viewport.CenterY,
                Resolution = viewport.Resolution
            });
        }

        base.OnClosing(e);
    }

    private void DragWindow(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void CloseWindow(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Minimize(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MapControlInfo(object? sender, MapInfoEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var mapInfo = e.GetMapInfo(mapControl.Map.Layers);

        if (mapInfo?.Feature is PointFeature feature)
        {
            if (feature["Marker"] is MarkerDto marker)
            {
                _isMarkerClicked = true;
                vm.SelectedMarker = marker;
                vm.ClosePanel1Command.Execute(null);
                vm.OpenPanel1Command.Execute(null);
                vm.ClearPendingMarker();
                return;
            }
        }

        if (e.WorldPosition != null)
        {
            var (lon, lat) = SphericalMercator.ToLonLat(e.WorldPosition.X, e.WorldPosition.Y);
            vm.SetPendingMarker(lon, lat); 
        }
    }

    private void Overlay_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.CloseListCommand.Execute(null);
        }
    }
}