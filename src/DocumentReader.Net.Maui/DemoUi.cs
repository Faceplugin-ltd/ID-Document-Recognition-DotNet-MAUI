using System.Text.Json;

namespace DocumentReader.Net.Maui;

static class DemoTheme
{
    public static readonly Color Bg = Color.FromArgb("#0F1419");
    public static readonly Color Card = Color.FromArgb("#5B2D8E");
    public static readonly Color Accent = Color.FromArgb("#3DD68C");
    public static readonly Color Text = Colors.White;
    public static readonly Color Muted = Color.FromArgb("#9AA4B2");
    public static readonly Color StatusInfo = Color.FromArgb("#1E3A5F");
    public static readonly Color StatusOk = Color.FromArgb("#14532D");
    public static readonly Color StatusError = Color.FromArgb("#7F1D1D");
    public static readonly Color Surface = Color.FromArgb("#1A222D");
    public static readonly Color Border = Color.FromArgb("#2A3441");
}

static class SdkSession
{
    public static bool Ready { get; private set; }
    public static bool Loading { get; private set; } = true;
    public static string Message { get; private set; } = "Loading native SDK…";

    public static async Task BootstrapAsync()
    {
        Loading = true;
        Ready = false;
        Message = "Loading native SDK…";
        try
        {
            await WaitForAndroidActivityAsync();
            await DocumentReaderSdk.GetMachineCodeAsync();
            var act = await DocumentReaderSdk.SetActivationAsync(LicenseKeys.Demo);
            if (act != DocumentReaderSdk.Success)
            {
                var detail = await DocumentReaderSdk.LastLicenseErrorAsync();
                Message = StatusLabel(act) + (string.IsNullOrWhiteSpace(detail) ? "" : ": " + detail);
                return;
            }

            var code = await DocumentReaderSdk.InitAsync();
            Message = StatusLabel(code);
            if (code == DocumentReaderSdk.Success)
            {
                var label = LicenseLabel(await DocumentReaderSdk.GetLicenseStatusAsync());
                Message = string.IsNullOrWhiteSpace(label) ? "Ready" : "Ready · " + label;
                Ready = true;
            }
        }
        catch (Exception ex)
        {
            Message = "Init error: " + ex.Message;
        }
        finally
        {
            Loading = false;
        }
    }

    static async Task WaitForAndroidActivityAsync()
    {
#if ANDROID
        for (var i = 0; i < 100 && Microsoft.Maui.ApplicationModel.Platform.CurrentActivity == null; i++)
        {
            await Task.Delay(50).ConfigureAwait(false);
        }
#endif
    }

    public static string StatusLabel(int code) => code switch
    {
        0 => "Ready",
        1 => "License invalid",
        2 => "License expired",
        3 => "Not activated",
        4 => "Init failed",
        5 => "No database found",
        6 => "Database loading error",
        _ => $"Failed ({code})",
    };

    public static string LicenseLabel(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            if (doc.RootElement.TryGetProperty("label", out var label) &&
                label.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(label.GetString()))
            {
                return label.GetString()!.Trim();
            }
        }
        catch (JsonException)
        {
        }

        return "Not licensed";
    }
}

static class DemoFlow
{
    public static async Task<byte[]?> ReadAsync(FileResult? photo)
    {
        if (photo == null)
        {
            return null;
        }

        await using var stream = await photo.OpenReadAsync();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        return memory.ToArray();
    }

    public static async Task ShowResultAsync(string json)
    {
        await ResultCache.PrepareAsync(json);
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Shell.Current.GoToAsync(nameof(ResultPage));
        });
    }
}
