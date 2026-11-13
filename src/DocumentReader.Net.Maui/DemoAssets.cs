using Microsoft.Maui.Controls.Shapes;

namespace DocumentReader.Net.Maui;

/// <summary>
/// Bundled logo + vector tile glyphs (same paths as Document Reader Android / RN demos).
/// </summary>
static class DemoAssets
{
    public const string Logo = "fp_logo.png";

    // Material-style paths from Android ic_camera / ic_gallery / ic_info (and RN TileIcons).
    const string CameraPath =
        "M9,2L7.17,4H4c-1.1,0 -2,0.9 -2,2v12c0,1.1 0.9,2 2,2h16c1.1,0 2,-0.9 2,-2V6c0,-1.1 -0.9,-2 -2,-2h-3.17L15,2H9zM12,17c-2.76,0 -5,-2.24 -5,-5s2.24,-5 5,-5 5,2.24 5,5 -2.24,5 -5,5zM12,9c-1.66,0 -3,1.34 -3,3s1.34,3 3,3 3,-1.34 3,-3 -1.34,-3 -3,-3z";
    const string GalleryPath =
        "M21,19V5c0,-1.1 -0.9,-2 -2,-2H5c-1.1,0 -2,0.9 -2,2v14c0,1.1 0.9,2 2,2h14c1.1,0 2,-0.9 2,-2zM8.5,13.5l2.5,3.01L14.5,12l4.5,6H5l3.5,-4.5z";
    const string AboutPath =
        "M12,2C6.48,2 2,6.48 2,12s4.48,10 10,10 10,-4.48 10,-10S17.52,2 12,2zM13,17h-2v-6h2v6zM13,9h-2V7h2v2z";

    public static View CreateLogo(double size = 120, bool tappable = true)
    {
        var image = new Image
        {
            Source = Logo,
            WidthRequest = size * 2.2,
            HeightRequest = size * 0.44,
            Aspect = Aspect.AspectFit,
            HorizontalOptions = LayoutOptions.Center,
        };
        if (!tappable)
        {
            return image;
        }

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await Launcher.Default.OpenAsync("https://faceplugin.com");
        image.GestureRecognizers.Add(tap);
        return image;
    }

    public static Border CreateTile(string title, string iconPathData, Action onTap)
    {
        var stack = new VerticalStackLayout
        {
            Padding = new Thickness(4, 12),
            Spacing = 8,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
            Children =
            {
                CreateGlyph(iconPathData),
                new Label
                {
                    Text = title,
                    TextColor = DemoTheme.Text,
                    FontSize = 12,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalTextAlignment = TextAlignment.Center,
                    LineBreakMode = LineBreakMode.TailTruncation,
                    MaxLines = 1,
                    InputTransparent = true,
                },
            },
        };
        var tile = new Border
        {
            BackgroundColor = DemoTheme.Card,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            HeightRequest = 112,
            Content = stack,
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => onTap();
        tile.GestureRecognizers.Add(tap);
        return tile;
    }

    public static Border CreateCameraTile(Action onTap) => CreateTile("CAMERA", CameraPath, onTap);

    public static Border CreateGalleryTile(Action onTap) => CreateTile("GALLERY", GalleryPath, onTap);

    public static Border CreateAboutTile(Action onTap) => CreateTile("ABOUT", AboutPath, onTap);

    static View CreateGlyph(string pathData, double size = 36)
    {
        var path = new Microsoft.Maui.Controls.Shapes.Path
        {
            Data = (Geometry)new PathGeometryConverter().ConvertFromInvariantString(pathData)!,
            Fill = DemoTheme.Text,
            Aspect = Stretch.Uniform,
            WidthRequest = size,
            HeightRequest = size,
            HorizontalOptions = LayoutOptions.Center,
            InputTransparent = true,
        };
        return new Grid
        {
            WidthRequest = size,
            HeightRequest = size,
            HorizontalOptions = LayoutOptions.Center,
            InputTransparent = true,
            Children = { path },
        };
    }
}
