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
    private bool _isDragging;
    
    public MainWindow()
    {
        InitializeComponent();

        mapControl.Info += MapControlInfo;
        mapControl.PointerPressed += OnMapPointerPressed;
        mapControl.PointerReleased += OnMapPointerReleased;

        var test = ImageService.GetImagePath("sign_6.png"); 
    }
    
    private void OnMapPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _pointerDownPosition = e.GetPosition(mapControl);
        _isDragging = false;
    }

    private void OnMapPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var currentPosition = e.GetPosition(mapControl);
        var delta = _pointerDownPosition - currentPosition;
    
        if (Math.Abs(delta.X) > 5 || Math.Abs(delta.Y) > 5)
        {
            return;
        }
    
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ClosePanel1Command.Execute(null);
            if (vm.IsPanel2Open)
            {
                vm.ClosePanel2Command.Execute(null);
            }
            else
            {
                vm.OpenPanel2Command.Execute(null);
            }
        }
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

    private void MapControl_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ClosePanel1Command.Execute(null);
            if (vm.IsPanel2Open)
            {
                vm.ClosePanel2Command.Execute(null);
            }
            else
            {
                vm.OpenPanel2Command.Execute(null);
            }   
        }
    }

    private void MapControlInfo(object? sender, MapInfoEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        // Получаем MapInfo через метод GetMapInfo
        var mapInfo = e.GetMapInfo(mapControl.Map.Layers);
    
        // Проверяем клик по существующему маркеру
        if (mapInfo?.Feature is PointFeature feature)
        {
            if (feature["Marker"] is MarkerDto marker)
            {
                vm.SelectedMarker = marker;
                vm.OpenPanel1Command.Execute(null);
                vm.ClearPendingMarker();
                return;
            }
        }

        // Получаем координаты клика из WorldPosition
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