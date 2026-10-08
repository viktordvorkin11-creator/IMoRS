using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using IMoRS.DTOs;
using IMoRS.Services;

namespace IMoRS.ViewModels;

public partial class MainWindowViewModel
{
    /// <summary>Создает и настраивает карту с загруженными метками.</summary>
    private void CreateMap()
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());
        
        _markerLayer.Name = "Markers";
        _markerLayer.Features = new List<IFeature>();
        
        LoadMarkersFromDb();
        
        map.Layers.Add(_markerLayer);
        
        var state = MapStateService.Load();
        var settings = SettingsService.Load();
        
        double centerX, centerY, resolution;
        
        if (settings.RestoreMapState
            && state != null
            && IsFinite(state.X) && IsFinite(state.Y)
            && IsFinite(state.Resolution) && state.Resolution > 0)
        {
            centerX = state.X;
            centerY = state.Y;
            resolution = state.Resolution;
        }
        else
        {
            (centerX, centerY) = SphericalMercator.FromLonLat(
                settings.DefaultMapX,
                settings.DefaultMapY);
            resolution = 156543.03392804097 / Math.Pow(2, settings.DefaultMapZoom);
        }
        
        map.Navigator.CenterOn(centerX, centerY);
        map.Navigator.ZoomTo(resolution);

        Map = map;
        Map.Refresh();   
    }
    
    private static bool IsFinite(double v)
        => !double.IsNaN(v) && !double.IsInfinity(v);


    private void RemovePendingMarkerLayer()
    {
        if (Map == null) return;
        var layer = Map.Layers.FirstOrDefault(x => x.Name == "PendingMarker");
        if (layer != null)
        {
            Map.Layers.Remove(layer);
            Map.Refresh();
        }
    }
}