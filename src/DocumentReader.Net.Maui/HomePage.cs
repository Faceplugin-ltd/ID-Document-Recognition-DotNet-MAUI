namespace DocumentReader.Net.Maui;

public class HomePage : ContentPage
{
    readonly Label _status;
    readonly Border _cameraTile;
    readonly Border _galleryTile;
    readonly Border _aboutTile;
    bool _started;
    bool _navigating;

    public HomePage()
    {
        BackgroundColor = DemoTheme.Bg;
        Shell.SetNavBarIsVisible(this, false);
        _status = new Label
        {
            Text = SdkSession.Message,
            TextColor = DemoTheme.Text,
            FontSize = 14,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };
        var statusBar = new Border
        {
            BackgroundColor = DemoTheme.StatusInfo,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(12, 12),
            MinimumHeightRequest = 64,
            Content = _status,
            Margin = new Thickness(16, 0, 16, 16),
        };
        _cameraTile = DemoAssets.CreateCameraTile(() => _ = Go(nameof(CameraPage)));
        _galleryTile = DemoAssets.CreateGalleryTile(() => _ = Go(nameof(GalleryPage)));
        _aboutTile = DemoAssets.CreateAboutTile(() => _ = Go(nameof(AboutPage), requiresSdk: false));
        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
            },
            Children =
            {
                new VerticalStackLayout
                {
                    Padding = new Thickness(16, 36, 16, 16),
                    Spacing = 16,
                    Children =
                    {
                        DemoAssets.CreateLogo(),
                        new Label
                        {
                            Text = "FacePlugin DocumentReader",
                            TextColor = DemoTheme.Text,
                            FontSize = 20,
                            FontAttributes = FontAttributes.Bold,
                            HorizontalTextAlignment = TextAlignment.Center,
                        },
                        new Grid
                        {
                            ColumnDefinitions =
                            {
                                new ColumnDefinition(GridLength.Star),
                                new ColumnDefinition(GridLength.Star),
                                new ColumnDefinition(GridLength.Star),
                            },
                            ColumnSpacing = 10,
                            Children = { _cameraTile, _galleryTile, _aboutTile },
                        },
                    },
                },
                statusBar,
            },
        };
        Grid.SetRow(statusBar, 1);
        Grid.SetColumn(_galleryTile, 1);
        Grid.SetColumn(_aboutTile, 2);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_started)
        {
            Paint();
            return;
        }

        _started = true;
        Paint();
        await SdkSession.BootstrapAsync();
        Paint();
    }

    void Paint()
    {
        _status.Text = SdkSession.Message;
        ((Border)_status.Parent).BackgroundColor = SdkSession.Ready
            ? DemoTheme.StatusOk
            : SdkSession.Loading ? DemoTheme.StatusInfo : DemoTheme.StatusError;
        var cameraGalleryOpacity = SdkSession.Ready && !SdkSession.Loading ? 1.0 : 0.45;
        _cameraTile.Opacity = cameraGalleryOpacity;
        _galleryTile.Opacity = cameraGalleryOpacity;
        _aboutTile.Opacity = SdkSession.Loading ? 0.65 : 1.0;
    }

    async Task Go(string route, bool requiresSdk = true)
    {
        if (_navigating)
        {
            return;
        }

        if (SdkSession.Loading)
        {
            await DisplayAlert("Please wait", SdkSession.Message, "OK");
            return;
        }

        if (requiresSdk && !SdkSession.Ready)
        {
            await DisplayAlert("SDK not ready", SdkSession.Message, "OK");
            return;
        }

        _navigating = true;
        try
        {
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Could not open screen", ex.Message, "OK");
        }
        finally
        {
            _navigating = false;
        }
    }
}
