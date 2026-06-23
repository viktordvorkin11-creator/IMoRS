using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
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

    [ObservableProperty] private bool _isPanel2Open;

    [ObservableProperty] private double borderListWidth = 25;

    [ObservableProperty] private double borderListHeight = 0;

    [ObservableProperty] private bool isListOpen;

    [ObservableProperty] private double overlayOpacity;

    [ObservableProperty] private MarkerDto? selectedMarker;

    [ObservableProperty] private Bitmap userImage;

    [ObservableProperty] private ObservableCollection<Bitmap> _filteredImages = new();

    [ObservableProperty] private int page;

    [ObservableProperty] private double iconsOpacity = 0;

    [ObservableProperty] private double appOpacity = 0;

    private const int PageSize = 50;

    private readonly MarkerService _markerService = new();

    private readonly MarkerService _database = new();

    private readonly List<IFeature> _markers = [];

    public ObservableCollection<Bitmap> Images { get; } = new();

    public int TotalPages => Images.Count == 0 ? 1 : (int)Math.Ceiling(Images.Count / (double)PageSize);
    public string PageInfo => $"{Page + 1} / {TotalPages}";

    partial void OnPageChanged(int value)
    {
        if (value < 0) value = 0;
        if (Images.Count > 0 && value >= TotalPages) value = TotalPages - 1;
        if (Images.Count == 0) value = 0;

        Page = value;
        UpdatePage();
    }

    private readonly Dictionary<string, int> _bitmapIds = new();

    public MainWindowViewModel()
    {
        AppOpacity = 0;
        IconsOpacity = 0;
        CreateMap();
        LoadImages();
        LoadApp();
    }

    private async Task LoadApp()
    {
        await Task.Delay(1000);
        AppOpacity = 1;
    }

    private void LoadImages()
    {
        var images = LoadAllImagesFromAssets("Assets/SignIconPng");

        foreach (var img in images)
        {
            Images.Add(img);
        }

        UpdatePage();
    }

    private void UpdatePage()
    {
        if (Images == null)
        {
            FilteredImages = new ObservableCollection<Bitmap>();
            return;
        }
        
        var pageItems = Images
            .Skip(Page * PageSize)
            .Take(PageSize)
            .ToList();
        
            FilteredImages = new ObservableCollection<Bitmap>(pageItems);
    }

    private List<Bitmap> LoadAllImagesFromAssets(string assetsFolderPath)
    {
        var bitmaps = new List<Bitmap>();
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        var folderUri = new Uri($"avares://{assemblyName}/{assetsFolderPath.TrimStart('/')}");
        var assetUris = AssetLoader.GetAssets(folderUri, null);

        Console.WriteLine(assetUris.Count());
        foreach (var assetUri in assetUris)
        {
            try
            {
                using (var assetStream = AssetLoader.Open(assetUri))
                {
                    bitmaps.Add(new Bitmap(assetStream));
                    Console.WriteLine(assetStream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки {assetUri}: {ex.Message}");
            }
        }

        return bitmaps;
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
            if (string.IsNullOrEmpty(marker.IconPath))
                continue;

            if (!File.Exists(marker.IconPath))
            {
                Console.WriteLine(marker.IconPath);
                continue;
            }

            var feature = new PointFeature(
                SphericalMercator.FromLonLat(marker.X, marker.Y));

            feature["Marker"] = marker;

            feature.Styles.Add(new ImageStyle
            {
                Image = $"file://{marker.IconPath}",
                SymbolScale = 0.5
            });

            _markers.Add(feature);
        }

        _markerLayer.Features = _markers;

        map.Layers.Add(_markerLayer);

        var state = MapStateService.Load();

        // if (state != null)
        // {
        //     map.Navigator.CenterOn(state.X, state.Y);
        //     map.Navigator.ZoomTo(state.Resolution);
        // }
        // else
        // {
            var (centerX, centerY) = SphericalMercator.FromLonLat(82.9204, 55.0302);
            map.Navigator.CenterOn(centerX, centerY);
            map.Navigator.ZoomTo(12);
        // }

        Map = map;
        Map.Navigator.ViewportChanged += OnViewportChanged;
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
        var feature = new PointFeature(
            SphericalMercator.FromLonLat(x, y));

        feature.Styles.Add(new SymbolStyle());

        _markers.Add(feature);

        _markerLayer.Features = _markers;

        _markerService.Add(x, y);

        Map?.Refresh();
    }

    [RelayCommand]
    private void RightPage()
    {
        Page += 1;
    }

    [RelayCommand]
    private void LeftPage()
    {
        Page -= 1;
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
        if (selectedMarker == null)
            return;

        UserImage = new Bitmap(selectedMarker.ImagePath);
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
        IsPanel2Open = false;
    }

    [RelayCommand]
    public void OpenPanel2()
    {
        IsPanel2Open = true;

        PanelWidth2 = App.MainWindow!.Bounds.Width * 0.1;
        ArrowAngle = 0;
    }

    [RelayCommand]
    public async Task OpenList()
    {
        ClosePanel1();
        ClosePanel2();
        OverlayOpacity = 0.67;
        BorderListHeight = App.MainWindow!.Bounds.Height * 0.7;
        await Task.Delay(200);
        BorderListWidth = App.MainWindow!.Bounds.Width * 0.7;
        await Task.Delay(200);
        IsListOpen = true;
        IconsOpacity = 1;
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
        IconsOpacity = 0;
    }

    [RelayCommand]
    public async Task AddImage()
    {
        if (App.MainWindow != null)
        {
            var files = await App.MainWindow.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "Выберите изображение",
                    AllowMultiple = false,
                    FileTypeFilter =
                    [
                        new FilePickerFileType("Изображения")
                        {
                            Patterns = ["*.jpg", "*.jpeg", "*.png"]
                        }
                    ]
                });
            if (files.Count == 0)
                return;


            var photoPath = files[0].Path.LocalPath;

            selectedMarker.ImagePath = photoPath;
            _markerService.UpdateApp(selectedMarker);

            UserImage = new Bitmap(photoPath);
        }
    }
}