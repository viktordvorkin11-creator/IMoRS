using Avalonia.Media.Imaging;

namespace IMoRS.DTOs;

public class MarkerDto
{
    public int Id { get; set; }

    public double X { get; set; }

    public double Y { get; set; }

    public string? Description { get; set; }

    public string? IconPath { get; set; }
    
    public string? ImagePath { get; set; }
}