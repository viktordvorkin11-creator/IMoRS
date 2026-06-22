using System;
using System.IO;

namespace IMoRS.Data;

public static class DbPath
{
    public static string GetPath()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IMoRS");

        Directory.CreateDirectory(folder);

        return Path.Combine(folder, "imors.db");
    }
}