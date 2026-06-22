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
    public MainWindow()
    {
        InitializeComponent();

        mapControl.Info += MapControlInfo;

        var test = ImageService.GetImagePath("sign_6.png"); 
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
            return;

        var mapInfo = e.GetMapInfo(e.Map.Layers);

        if (mapInfo?.Feature is PointFeature feature)
        {
            if (feature["Marker"] is MarkerDto marker)
            {
                vm.SelectedMarker = marker;

                vm.OpenPanel1Command.Execute(null);
            }
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