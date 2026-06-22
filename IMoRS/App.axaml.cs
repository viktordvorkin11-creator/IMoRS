using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using IMoRS.Data;
using IMoRS.ViewModels;
using IMoRS.Views;
using Microsoft.EntityFrameworkCore;

namespace IMoRS;

public partial class App : Application
{
    public static MainWindow? MainWindow { get; private set; }
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var dbContext = new AppDbContext();
        dbContext.Database.Migrate();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow = new MainWindow();
            MainWindow.DataContext = new MainWindowViewModel();
            desktop.MainWindow = MainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}