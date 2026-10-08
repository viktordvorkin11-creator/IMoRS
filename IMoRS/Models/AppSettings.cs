namespace IMoRS.Models;

public class AppSettings
{
    public double DefaultMarkerScale { get; set; } = 0.2;
    
    public int MaxIconDimension { get; set; } = 512;    

    public double DefaultMapX { get; set; } = 82.9204;
    public double DefaultMapY { get; set; } = 55.0302;
    public double DefaultMapZoom { get; set; } = 12;
    public bool RestoreMapState { get; set; } = true;

    public bool ConfirmDelete { get; set; } = true;
}