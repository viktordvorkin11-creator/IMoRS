using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using IMoRS.Models;
using IMoRS.Services;
using IMoRS.ViewModels;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using Mapsui.UI.Avalonia;

namespace IMoRS.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        mapControl.Info += MapControlInfo;
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
            vm.ClosePanel2Command.Execute(null);
        }
    }

    private void MapControlInfo(object? sender, MapInfoEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        
        var worldPosition = e.WorldPosition;

        var (lon, lat) = SphericalMercator.ToLonLat(
            worldPosition.X,
            worldPosition.Y);

        vm.AddMarker(lon, lat);
    }
}