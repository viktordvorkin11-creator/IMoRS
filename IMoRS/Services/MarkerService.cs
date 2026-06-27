using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using IMoRS.Data;
using IMoRS.Data.Entities;
using IMoRS.DTOs;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;

namespace IMoRS.Services;

public class MarkerService
{
    public MarkerDto Add(double x, double y, string iconPath, double scale)
    {
        using var db = new AppDbContext();

        var marker = new MarkerEntity
        {
            X = x,
            Y = y,
            IconPath = iconPath,
            ImagePath = iconPath,
            Scale = scale
        };

        db.Markers.Add(marker);
        db.SaveChanges();

        return new MarkerDto
        {
            Id = marker.Id,
            X = marker.X,
            Y = marker.Y,
            IconPath = marker.IconPath,
            Scale = marker.Scale
        };
    }
    
    public List<MarkerDto> GetAll()
    {
        using var db = new AppDbContext();

        return db.Markers
            .Select(m => new MarkerDto
            {
                Id = m.Id,
                X = m.X,
                Y = m.Y,
                Description = m.Description,
                IconPath = m.IconPath,
                Scale = m.Scale
            })
            .ToList();
    }

    public void UpdateApp(MarkerDto dto)
    {
        using var db = new AppDbContext();

        var entity = db.Markers.Find(dto.Id);

        if (entity == null) return;

        entity.X = dto.X;
        entity.Y = dto.Y;
        entity.Description = dto.Description;
        entity.IconPath = dto.IconPath ?? string.Empty;
        entity.Scale = dto.Scale;

        db.SaveChanges();
    }

    public void UpdateAllScales(double newScale)
    {
        using var db = new AppDbContext();
        var markers = db.Markers.ToList();

        foreach (var marker in markers)
        {
            marker.Scale = newScale;
        }
        
        db.SaveChanges();
    }
    
    public void Delete(int markerId)
    {
        Console.WriteLine($"Delete Id = {markerId}");
        using var db = new AppDbContext();

        var marker = db.Markers.Find(markerId);

        if (marker == null)
            return;

        db.Markers.Remove(marker);
        db.SaveChanges();
    }
}