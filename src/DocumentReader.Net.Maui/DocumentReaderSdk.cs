namespace DocumentReader.Net.Maui;

/// <summary>
/// Same call order as the Android and iOS Document Reader SDKs:
/// getMachineCode → setActivation → init → recognize.
/// 0 is success.
/// </summary>
public static partial class DocumentReaderSdk
{
    public const int Success = 0;
    public const int LicenseInvalid = 1;
    public const int LicenseExpired = 2;
    public const int NotActivated = 3;
    public const int InitFailed = 4;

    public static partial Task<string> GetMachineCodeAsync();

    public static partial Task<int> SetActivationAsync(string license);

    public static partial Task<int> InitAsync();

    public static partial Task<string> GetLicenseStatusAsync();

    public static partial Task<string> LastLicenseErrorAsync();

    /// <summary>
    /// Still OCR / MRZ / barcode. <paramref name="authenticityMode"/> is "normal" or "none".
    /// </summary>
    public static partial Task<string> RecognizeAsync(byte[] frontJpeg, byte[]? backJpeg, string authenticityMode);

    /// <summary>Live camera locate overlay (same JSON as Flutter/RN locateDocument).</summary>
    public static partial Task<string> LocateDocumentAsync(byte[] jpeg);

    public static partial Task DeinitAsync();
}
