namespace DocumentReader.Net.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(GalleryPage), typeof(GalleryPage));
        Routing.RegisterRoute(nameof(CameraPage), typeof(CameraPage));
        Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
        Routing.RegisterRoute(nameof(ResultPage), typeof(ResultPage));
    }
}
