namespace DocumentReader.Net.Maui;

public class ResultPage : ContentPage
{
    readonly Grid _tabs = new()
    {
        ColumnDefinitions =
        {
            new ColumnDefinition(GridLength.Star),
            new ColumnDefinition(GridLength.Star),
            new ColumnDefinition(GridLength.Star),
            new ColumnDefinition(GridLength.Star),
        },
        ColumnSpacing = 4,
        Padding = 4,
        BackgroundColor = DemoTheme.Surface,
    };
    readonly ContentView _body = new();
    int _tab;

    public ResultPage()
    {
        Title = "Result";
        BackgroundColor = DemoTheme.Bg;
        Shell.SetBackgroundColor(this, DemoTheme.Bg);
        Shell.SetTitleColor(this, DemoTheme.Text);
        Shell.SetForegroundColor(this, DemoTheme.Text);
        var labels = new[] { "Result", "Liveness", "Images", "Raw JSON" };
        for (var i = 0; i < labels.Length; i++)
        {
            var index = i;
            var button = new Button
            {
                Text = labels[i],
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 8,
                Padding = new Thickness(2, 10),
            };
            button.Clicked += (_, _) =>
            {
                _tab = index;
                Paint();
            };
            Grid.SetColumn(button, i);
            _tabs.Children.Add(button);
        }

        var tabHost = new Border
        {
            Margin = new Thickness(12, 8, 12, 0),
            StrokeThickness = 0,
            BackgroundColor = DemoTheme.Surface,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            Content = _tabs,
        };
        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
            },
            Children = { tabHost, _body },
        };
        Grid.SetRow(_body, 1);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Paint();
    }

    void Paint()
    {
        for (var i = 0; i < _tabs.Children.Count; i++)
        {
            var button = (Button)_tabs.Children[i];
            var active = i == _tab;
            button.BackgroundColor = active ? DemoTheme.Accent : Colors.Transparent;
            button.TextColor = active ? Colors.Black : DemoTheme.Text;
        }

        _body.Content = new ScrollView
        {
            Padding = new Thickness(16, 16, 16, 40),
            Content = _tab switch
            {
                0 => ResultTab(),
                1 => SecurityTab(),
                2 => ImagesTab(),
                _ => RawTab(),
            },
        };
    }

    static VerticalStackLayout ResultTab()
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Add(new Label
        {
            Text = ResultCache.Summary,
            TextColor = DemoTheme.Text,
            FontSize = 15,
        });
        stack.Add(Header("Src", "Field", "Value"));
        var index = 0;
        foreach (var row in ResultCache.FieldRows)
        {
            stack.Add(FieldLine(row.Source, row.Key, row.Value, index++ % 2 == 0));
        }

        return stack;
    }

    static VerticalStackLayout SecurityTab()
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Add(new Label
        {
            Text = ResultCache.SecuritySummary,
            TextColor = DemoTheme.Text,
            FontSize = 15,
        });
        if (ResultCache.SecurityRows.Count == 0)
        {
            stack.Add(new Label { Text = "No liveness checks in this response", TextColor = DemoTheme.Muted });
            return stack;
        }

        stack.Add(Header("Page", "Check", "Status"));
        var index = 0;
        foreach (var row in ResultCache.SecurityRows)
        {
            stack.Add(FieldLine(row.Page, row.Check, row.Status, index++ % 2 == 0));
        }

        return stack;
    }

    static View ImagesTab()
    {
        if (ResultCache.Images.Count == 0)
        {
            return new Label { Text = "No images in response.", TextColor = DemoTheme.Muted };
        }

        var stack = new VerticalStackLayout { Spacing = 16 };
        foreach (var image in ResultCache.Images)
        {
            var title = image.Category + (image.Source.Length == 0 ? "" : " · " + image.Source);
            stack.Add(new Label { Text = title, TextColor = DemoTheme.Text, FontAttributes = FontAttributes.Bold });
            stack.Add(new Image
            {
                Source = ImageSource.FromStream(() => new MemoryStream(image.Bytes)),
                Aspect = Aspect.AspectFit,
                HeightRequest = 220,
                BackgroundColor = DemoTheme.Surface,
            });
        }

        return stack;
    }

    static Label RawTab() => new()
    {
        Text = ResultCache.Pretty,
        TextColor = DemoTheme.Text,
        FontSize = 12,
        FontFamily = "Consolas",
    };

    static Grid Header(string a, string b, string c) => FieldLine(a, b, c, false, header: true);

    static Grid FieldLine(string a, string b, string c, bool alt, bool header = false)
    {
        var colorA = header || a.Length < 12 ? DemoTheme.Muted : DemoTheme.Muted;
        var colorB = header ? DemoTheme.Muted : DemoTheme.Text;
        var colorC = header ? DemoTheme.Muted : DemoTheme.Accent;
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(64)),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(new GridLength(1.4, GridUnitType.Star)),
            },
            Padding = new Thickness(0, 8),
            BackgroundColor = alt ? Color.FromRgba(255, 255, 255, 8) : Colors.Transparent,
            Children =
            {
                Cell(a, colorA, header),
                Cell(b, colorB, header),
                Cell(c, colorC, header),
            },
        };
        Grid.SetColumn((View)grid.Children[1], 1);
        Grid.SetColumn((View)grid.Children[2], 2);
        return grid;
    }

    static Label Cell(string text, Color color, bool header) => new()
    {
        Text = text,
        TextColor = color,
        FontSize = header ? 12 : 13,
        FontAttributes = header ? FontAttributes.Bold : FontAttributes.None,
    };
}
