using Camera.MAUI;

namespace DocumentReader.Net.Maui;

public class CameraPage : ContentPage
{
    const int HighThreshold = 85;

    readonly CameraView _camera = new() { HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill };
    readonly DocumentOverlayDrawable _overlayDrawable = new();
    readonly GraphicsView _overlay;
    readonly Label _hint = new() { Text = "Align the ID inside the frame", TextColor = Colors.White, FontSize = 15, HorizontalTextAlignment = TextAlignment.Center };
    readonly Label _score = new() { Text = "0%", TextColor = Colors.Black, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
    readonly Button _capture;
    readonly ActivityIndicator _busy = new() { Color = Colors.Black, IsRunning = false, IsVisible = false };
    LocateSession? _session;
    bool _captureEnabled;
    bool _recognizing;
    bool _startingPreview;
    bool _cameraAttached;
    bool _visible;
    int _lifetime;
    TaskCompletionSource<bool>? _camerasLoaded;
    IReadOnlyList<DocPoint>? _corners;

    public CameraPage()
    {
        Title = "Camera";
        BackgroundColor = Colors.Black;
        Shell.SetBackgroundColor(this, DemoTheme.Bg);
        Shell.SetTitleColor(this, DemoTheme.Text);
        Shell.SetForegroundColor(this, DemoTheme.Text);
        _overlay = new GraphicsView
        {
            Drawable = _overlayDrawable,
            InputTransparent = true,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
        };
        _capture = new Button
        {
            Text = "Capture",
            TextColor = Colors.Black,
            BackgroundColor = DemoTheme.Accent,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 52,
            CornerRadius = 26,
            HorizontalOptions = LayoutOptions.Center,
            WidthRequest = 160,
            IsEnabled = false,
            Opacity = 0.45,
        };
        _capture.Clicked += OnCapture;
        var close = new Button
        {
            Text = "✕",
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#66000000"),
            FontSize = 18,
            WidthRequest = 44,
            HeightRequest = 44,
            CornerRadius = 22,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 8, 8, 0),
        };
        close.Clicked += async (_, _) => await Shell.Current.GoToAsync("..");
        var scoreBadge = new Border
        {
            BackgroundColor = DemoTheme.Accent,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
            WidthRequest = 72,
            HeightRequest = 36,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 56, 16, 0),
            Content = _score,
        };
        // CameraView stays out of the tree until the page is visible.
        // Creating it during Shell navigation blocks the UI thread, so the page never appears.
        Content = new Grid
        {
            Children =
            {
                _overlay,
                close,
                scoreBadge,
                new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.End,
                    Padding = new Thickness(16, 0, 16, 24),
                    Spacing = 12,
                    Children =
                    {
                        _hint,
                        new Grid { Children = { _capture, _busy } },
                    },
                },
            },
        };
        _camera.CamerasLoaded += OnCamerasLoaded;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _visible = true;
        var life = ++_lifetime;
        var grid = (Grid)Content;
        grid.SizeChanged -= OnPreviewSized;
        grid.SizeChanged += OnPreviewSized;
        _session?.Dispose();
        _session = new LocateSession(CaptureFrameAsync, OnLocateFrame);
        _session.UpdateViewSize(grid.Width, grid.Height);
        Dispatcher.Dispatch(async () =>
        {
            if (life != _lifetime || !_visible)
            {
                return;
            }

            AttachCamera(grid);
            await StartPreviewAsync();
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _visible = false;
        _lifetime++;
        _camerasLoaded?.TrySetResult(false);
        _session?.Stop();
        _session?.Dispose();
        _session = null;
        if (_cameraAttached)
        {
            _ = _camera.StopCameraAsync();
        }
    }

    void AttachCamera(Grid grid)
    {
        if (_cameraAttached)
        {
            return;
        }

        grid.Insert(0, _camera);
        _cameraAttached = true;
    }

    void OnPreviewSized(object? sender, EventArgs e)
    {
        if (sender is VisualElement view && view.Width > 0 && view.Height > 0)
        {
            _session?.UpdateViewSize(view.Width, view.Height);
        }
    }

    void OnCamerasLoaded(object? sender, EventArgs e) => _camerasLoaded?.TrySetResult(true);

    async Task StartPreviewAsync()
    {
        if (!_visible || _startingPreview)
        {
            return;
        }

        _startingPreview = true;
        try
        {
            if (_camera.NumCamerasDetected == 0)
            {
                _hint.Text = "Starting camera…";
                var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _camerasLoaded = ready;
                if (_camera.NumCamerasDetected == 0)
                {
                    await Task.WhenAny(ready.Task, Task.Delay(5000));
                }

                if (!_visible || _camera.NumCamerasDetected == 0)
                {
                    if (_visible)
                    {
                        _hint.Text = "No back camera found";
                    }

                    return;
                }
            }

            var back = _camera.Cameras.FirstOrDefault(c => c.Position == CameraPosition.Back)
                ?? _camera.Cameras.First();
            _camera.Camera = back;

            var permission = await Permissions.RequestAsync<Permissions.Camera>();
            if (permission != PermissionStatus.Granted)
            {
                _hint.Text = "Camera permission is required";
                return;
            }

            if (await _camera.StartCameraAsync(new Size(1280, 720)) == CameraResult.Success)
            {
                _session?.SetPreviewSize(1280, 720);
                _session?.Start();
                _hint.Text = "Align the ID inside the frame";
            }
            else
            {
                _hint.Text = "Could not start camera preview";
            }
        }
        catch (Exception ex)
        {
            if (_visible)
            {
                _hint.Text = ex.Message;
            }
        }
        finally
        {
            _startingPreview = false;
        }
    }

    async Task<byte[]?> CaptureFrameAsync()
    {
        var path = Path.Combine(FileSystem.CacheDirectory, $"loc_{Environment.TickCount64}.jpg");
        try
        {
            if (!await _camera.SaveSnapShot(Camera.MAUI.ImageFormat.JPEG, path))
            {
                return null;
            }

            return await File.ReadAllBytesAsync(path);
        }
        finally
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }
    }

    void OnLocateFrame(LocateFrame frame)
    {
        _score.Text = frame.ScorePct + "%";
        _corners = frame.Show ? frame.Corners : null;
        _overlayDrawable.Corners = _corners;
        _overlayDrawable.Locked = frame.ScorePct >= HighThreshold && _corners != null;
        _overlay.Invalidate();
        _captureEnabled = frame.ScorePct >= 50 && _session?.LatestCapture != null;
        if (_captureEnabled && !_recognizing)
        {
            _capture.IsEnabled = true;
            _capture.Opacity = 1;
            _hint.Text = frame.High ? "Ready — tap Capture or keep holding" : "Align the ID inside the frame";
        }
        else if (!_recognizing)
        {
            _capture.IsEnabled = false;
            _capture.Opacity = 0.45;
            _hint.Text = "Align the ID inside the frame";
        }
    }

    async void OnCapture(object? sender, EventArgs e)
    {
        var jpeg = _session?.LatestCapture;
        if (jpeg == null || _recognizing)
        {
            return;
        }

        _recognizing = true;
        _session?.Stop();
        _capture.IsEnabled = false;
        _capture.IsVisible = false;
        _busy.IsVisible = true;
        _busy.IsRunning = true;
        _hint.Text = "Reading document…";
        try
        {
            var json = await DocumentReaderSdk.RecognizeAsync(jpeg, null, "normal");
            await DemoFlow.ShowResultAsync(json);
        }
        catch (Exception ex)
        {
            _hint.Text = ex.Message;
            _capture.IsVisible = true;
            _capture.IsEnabled = _captureEnabled;
            _session?.Start();
        }
        finally
        {
            _busy.IsRunning = false;
            _busy.IsVisible = false;
            _recognizing = false;
        }
    }
}
