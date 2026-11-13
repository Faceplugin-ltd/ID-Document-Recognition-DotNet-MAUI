using Android.Graphics;
using Sdk = Com.Faceplugin.Documentreadersdk.DocumentReaderSDK;

namespace DocumentReader.Net.Maui;

public static partial class DocumentReaderSdk
{
    public static partial Task<string> GetMachineCodeAsync() =>
        SdkGate.RunAsync(() => Task.Run(() => Sdk.GetMachineCode(AppContext()) ?? ""));

    public static partial Task<int> SetActivationAsync(string license) =>
        SdkGate.RunAsync(() => Task.Run(() => Sdk.SetActivation(AppContext(), license)));

    public static partial Task<int> InitAsync() =>
        SdkGate.RunAsync(() => Task.Run(() => Sdk.Init(AppContext())));

    public static partial Task<string> GetLicenseStatusAsync() =>
        SdkGate.RunAsync(() => Task.Run(() => Sdk.LicenseStatus ?? ""));

    public static partial Task<string> LastLicenseErrorAsync() =>
        SdkGate.RunAsync(() => Task.Run(() => Sdk.LastLicenseError() ?? ""));

    public static partial Task<string> LocateDocumentAsync(byte[] jpeg) =>
        SdkGate.RunAsync(() => Task.Run(() =>
        {
            using var bitmap = BitmapFactory.DecodeByteArray(jpeg, 0, jpeg.Length)
                ?? throw new InvalidOperationException("Could not decode camera frame.");
            return Sdk.LocateDocument(bitmap) ?? "{}";
        }));

    public static partial Task<string> RecognizeAsync(byte[] frontJpeg, byte[]? backJpeg, string authenticityMode) =>
        SdkGate.RunAsync(() => Task.Run(() =>
        {
            using var front = BitmapFactory.DecodeByteArray(frontJpeg, 0, frontJpeg.Length)
                ?? throw new InvalidOperationException("Could not decode the front image.");
            using var back = backJpeg is { Length: > 0 }
                ? BitmapFactory.DecodeByteArray(backJpeg, 0, backJpeg.Length)
                : null;
            return Sdk.Recognize(front, back, authenticityMode) ?? "{\"msg\":\"empty recognize result\"}";
        }));

    public static partial Task DeinitAsync() =>
        SdkGate.RunAsync(() => Task.Run(Sdk.Deinit));

    static global::Android.Content.Context AppContext() =>
        Platform.CurrentActivity ?? global::Android.App.Application.Context;
}
