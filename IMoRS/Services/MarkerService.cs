using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;
using IMoRS.Data;
using IMoRS.Data.Entities;
using IMoRS.DTOs;

namespace IMoRS.Services;

public class MarkerService
{
    public void Add(double x, double y)
    {
        using var db = new AppDbContext();

        db.Markers.Add(new MarkerEntity
        {
            X = x,
            Y = y
        });

        db.SaveChanges();
    }

    public List<MarkerDto> GetAll()
    {
        using var db = new AppDbContext();

        return db.Markers
            .Select(m => new MarkerDto
            {
                X = m.X,
                Y = m.Y,
                Description = m.Description,
                ImagePath = m.ImagePath
            })
            .ToList();
    }
}