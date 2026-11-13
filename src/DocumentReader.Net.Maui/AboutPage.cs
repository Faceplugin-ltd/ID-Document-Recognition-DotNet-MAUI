namespace DocumentReader.Net.Maui;

public class AboutPage : ContentPage
{
    readonly Label _license = new()
    {
        Text = "License: …",
        TextColor = DemoTheme.Text,
        HorizontalTextAlignment = TextAlignment.Center,
    };

    public AboutPage()
    {
        Title = "About";
        BackgroundColor = DemoTheme.Bg;
        Shell.SetBackgroundColor(this, DemoTheme.Bg);
        Shell.SetTitleColor(this, DemoTheme.Text);
        Shell.SetForegroundColor(this, DemoTheme.Text);
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 12,
                Children =
                {
                    DemoAssets.CreateLogo(120, tappable: true),
                    new Label
                    {
                        Text = "FacePlugin",
                        TextColor = DemoTheme.Text,
                        FontSize = 22,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center,
                    },
                    new Label
                    {
                        Text = "Document Reader SDK",
                        TextColor = DemoTheme.Accent,
                        FontSize = 15,
                        HorizontalTextAlignment = TextAlignment.Center,
                    },
                    Card(_license),
                    Card(new Label
                    {
                        Text = "FacePlugin builds on-device identity technology — document recognition, face matching, and liveness — so biometric data never has to leave the phone.",
                        TextColor = DemoTheme.Text,
                    }),
                    Card(new Label
                    {
                        Text = "This app demos the Document Reader SDK for .NET MAUI: ID cards, passports, and driver licenses with OCR, MRZ, barcode, and optional authenticity checks. Everything runs fully on-premise.",
                        TextColor = DemoTheme.Text,
                    }),
                    new Label
                    {
                        Text = "faceplugin.com",
                        TextColor = DemoTheme.Accent,
                        FontSize = 16,
                        HorizontalTextAlignment = TextAlignment.Center,
                    },
                    new Label
                    {
                        Text = "© 2026 FacePlugin. All rights reserved.",
                        TextColor = DemoTheme.Muted,
                        HorizontalTextAlignment = TextAlignment.Center,
                    },
                },
            },
        };
        var link = (Label)((VerticalStackLayout)((ScrollView)Content).Content).Children[6];
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await Launcher.Default.OpenAsync("https://faceplugin.com");
        link.GestureRecognizers.Add(tap);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!SdkSession.Ready)
        {
            _license.Text = SdkSession.Loading ? "License: loading SDK…" : "License: " + SdkSession.Message;
            return;
        }

        try
        {
            var label = SdkSession.LicenseLabel(await DocumentReaderSdk.GetLicenseStatusAsync());
            _license.Text = "License: " + label;
        }
        catch (Exception ex)
        {
            _license.Text = "License: " + ex.Message;
        }
    }

    static Border Card(View content) => new()
    {
        BackgroundColor = DemoTheme.Surface,
        StrokeThickness = 0,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
        Padding = 16,
        Content = content,
    };
}
