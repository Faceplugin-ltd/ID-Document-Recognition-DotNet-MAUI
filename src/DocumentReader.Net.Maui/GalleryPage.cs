namespace DocumentReader.Net.Maui;

public class GalleryPage : ContentPage
{
    readonly Image _frontImage = PreviewImage();
    readonly Image _backImage = PreviewImage();
    readonly Label _frontHint = Hint("Tap to pick");
    readonly Label _backHint = Hint("Tap to pick");
    readonly Label _error = new() { TextColor = Color.FromArgb("#F87171"), IsVisible = false };
    readonly Button _recognize;
    byte[]? _front;
    byte[]? _back;
    bool _busy;

    public GalleryPage()
    {
        Title = "Gallery";
        BackgroundColor = DemoTheme.Bg;
        Shell.SetBackgroundColor(this, DemoTheme.Bg);
        Shell.SetTitleColor(this, DemoTheme.Text);
        Shell.SetForegroundColor(this, DemoTheme.Text);
        _recognize = new Button
        {
            Text = "Recognize",
            TextColor = Colors.Black,
            BackgroundColor = DemoTheme.Accent,
            FontAttributes = FontAttributes.Bold,
            FontSize = 16,
            HeightRequest = 52,
            CornerRadius = 12,
            IsEnabled = false,
            Opacity = 0.45,
        };
        _recognize.Clicked += OnRecognize;
        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 16,
            Children =
            {
                new Label
                {
                    Text = "Front image is required. Back is optional (ID cards).",
                    TextColor = DemoTheme.Muted,
                },
                new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Star),
                    },
                    ColumnSpacing = 12,
                    HeightRequest = 280,
                    Children =
                    {
                        Card("Front", _frontImage, _frontHint, PickFront, ClearFront),
                        Card("Back (optional)", _backImage, _backHint, PickBack, ClearBack),
                    },
                },
                _error,
                _recognize,
            },
        };
        var row = (Grid)((VerticalStackLayout)Content).Children[1];
        Grid.SetColumn((View)row.Children[1], 1);
    }

    static Image PreviewImage() => new()
    {
        Aspect = Aspect.AspectFill,
        IsVisible = false,
    };

    static Label Hint(string text) => new()
    {
        Text = text,
        TextColor = DemoTheme.Muted,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center,
    };

    static Grid Card(string label, Image image, Label hint, EventHandler pick, EventHandler clear)
    {
        var clearButton = new Button
        {
            Text = "Clear",
            TextColor = DemoTheme.Accent,
            BackgroundColor = Colors.Transparent,
            HeightRequest = 36,
        };
        clearButton.Clicked += clear;
        var preview = new Border
        {
            Stroke = DemoTheme.Border,
            BackgroundColor = DemoTheme.Surface,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            HeightRequest = 200,
            Content = new Grid { Children = { image, hint } },
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += (s, e) => pick(s, e);
        preview.GestureRecognizers.Add(tap);
        var labelView = new Label { Text = label, TextColor = DemoTheme.Text, FontAttributes = FontAttributes.Bold };
        Grid.SetRow(preview, 1);
        Grid.SetRow(clearButton, 2);
        return new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
            },
            RowSpacing = 8,
            Children = { labelView, preview, clearButton },
        };
    }

    async Task<bool> EnsurePhotoAccessAsync()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            return true;
        }

        var photos = await Permissions.RequestAsync<Permissions.Photos>();
        if (photos is PermissionStatus.Granted or PermissionStatus.Limited)
        {
            return true;
        }

        // Android 12 and earlier can still return a content URI from the system picker.
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            return true;
        }

        _error.Text = "Photo library permission is required.";
        _error.IsVisible = true;
        return false;
    }

    async void PickFront(object? sender, EventArgs e) => await Pick(true);

    async void PickBack(object? sender, EventArgs e) => await Pick(false);

    async Task Pick(bool front)
    {
        if (!await EnsurePhotoAccessAsync())
        {
            return;
        }

        var photo = await MediaPicker.Default.PickPhotoAsync();
        var bytes = await DemoFlow.ReadAsync(photo);
        if (bytes == null)
        {
            return;
        }

        if (front)
        {
            _front = bytes;
            Show(_frontImage, _frontHint, bytes);
        }
        else
        {
            _back = bytes;
            Show(_backImage, _backHint, bytes);
        }

        _error.IsVisible = false;
        var enabled = _front != null && !_busy;
        _recognize.IsEnabled = enabled;
        _recognize.Opacity = enabled ? 1 : 0.45;
    }

    static void Show(Image image, Label hint, byte[] bytes)
    {
        image.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
        image.IsVisible = true;
        hint.IsVisible = false;
    }

    void ClearFront(object? sender, EventArgs e) => Clear(true);

    void ClearBack(object? sender, EventArgs e) => Clear(false);

    void Clear(bool front)
    {
        if (front)
        {
            _front = null;
            _frontImage.IsVisible = false;
            _frontHint.IsVisible = true;
        }
        else
        {
            _back = null;
            _backImage.IsVisible = false;
            _backHint.IsVisible = true;
        }

        var enabled = _front != null && !_busy;
        _recognize.IsEnabled = enabled;
        _recognize.Opacity = enabled ? 1 : 0.45;
    }

    async void OnRecognize(object? sender, EventArgs e)
    {
        if (_front == null || _busy)
        {
            return;
        }

        _busy = true;
        _recognize.IsEnabled = false;
        _recognize.Text = "Recognizing…";
        _error.IsVisible = false;
        try
        {
            var json = await DocumentReaderSdk.RecognizeAsync(_front!, _back, "normal");
            await DemoFlow.ShowResultAsync(json);
        }
        catch (Exception ex)
        {
            _error.Text = ex.Message;
            _error.IsVisible = true;
        }
        finally
        {
            _busy = false;
            _recognize.Text = "Recognize";
            var enabled = _front != null;
            _recognize.IsEnabled = enabled;
            _recognize.Opacity = enabled ? 1 : 0.45;
        }
    }
}
