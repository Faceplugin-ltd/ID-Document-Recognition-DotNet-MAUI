using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace DocumentReader.Net.Maui;

/// <summary>
/// Windows calls DocumentReaderSDK.dll in this process. The runtime lives in lib/cpu
/// beside the executable. This app does not call another repository.
/// </summary>
public static partial class DocumentReaderSdk
{
    const int MachineCodeBytes = 768;
    const int StatusBytes = 512;
    const int ResultBytes = 8 * 1024 * 1024;

    static readonly object Gate = new();
    static bool _loaded;
    static string _lastError = "";

    public static partial Task<string> GetMachineCodeAsync() =>
        Task.Run(() =>
        {
            lock (Gate)
            {
                EnsureLoaded();
                var buffer = new byte[MachineCodeBytes];
                var code = Native.DocSDK_getMachineCode(buffer);
                if (code != Success)
                {
                    _lastError = "getMachineCode failed";
                    return "";
                }

                return Decode(buffer);
            }
        });

    public static partial Task<int> SetActivationAsync(string license) =>
        Task.Run(() =>
        {
            lock (Gate)
            {
                EnsureLoaded();
                var key = ResolveLicense(license);
                var bytes = Encoding.UTF8.GetBytes(key + "\0");
                var code = Native.DocSDK_activate(bytes);
                if (code != Success)
                {
                    _lastError = $"activate returned {code}";
                }

                return code;
            }
        });

    public static partial Task<int> InitAsync() =>
        Task.Run(() =>
        {
            lock (Gate)
            {
                EnsureLoaded();
                var code = Native.DocSDK_initialize();
                if (code < 0)
                {
                    _lastError = "init failed";
                    return InitFailed;
                }

                if (code != Success)
                {
                    _lastError = $"init returned {code}";
                }

                return code;
            }
        });

    public static partial Task<string> GetLicenseStatusAsync() =>
        Task.Run(() =>
        {
            lock (Gate)
            {
                EnsureLoaded();
                var buffer = new byte[StatusBytes];
                Native.DocSDK_getLicenseStatus(buffer);
                return Decode(buffer);
            }
        });

    public static partial Task<string> LastLicenseErrorAsync() =>
        Task.FromResult(_lastError);

    public static partial Task<string> LocateDocumentAsync(byte[] jpeg) =>
        Task.FromResult("{\"score\":0,\"msg\":\"Camera locate is mobile-only in this demo\"}");

    public static partial Task<string> RecognizeAsync(byte[] frontJpeg, byte[]? backJpeg, string authenticityMode) =>
        Task.Run(() =>
        {
            lock (Gate)
            {
                EnsureLoaded();
                var images = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["image"] = Convert.ToBase64String(frontJpeg),
                        ["page_idx"] = 0,
                    },
                };
                if (backJpeg is { Length: > 0 })
                {
                    images.Add(new Dictionary<string, object>
                    {
                        ["image"] = Convert.ToBase64String(backJpeg),
                        ["page_idx"] = 1,
                    });
                }

                var options = new Dictionary<string, object>
                {
                    ["response"] = new Dictionary<string, string>
                    {
                        ["OCR"] = "normal",
                        ["MRZ"] = "normal",
                        ["Barcode"] = "normal",
                        ["ImageQuality"] = "normal",
                        ["Authenticity"] = authenticityMode,
                    },
                };
                var imagesJson = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(images) + "\0");
                var optionsJson = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(options) + "\0");
                var rfid = new byte[] { 0 };
                var result = new byte[ResultBytes];
                Native.DocSDK_documentProcess(imagesJson, rfid, optionsJson, result);
                var text = Decode(result);
                return string.IsNullOrWhiteSpace(text) ? "{\"msg\":\"empty recognize result\"}" : text;
            }
        });

    public static partial Task DeinitAsync() =>
        Task.Run(() =>
        {
            lock (Gate)
            {
                if (!_loaded)
                {
                    return;
                }

                var result = new byte[256];
                Native.DocSDK_unload(result);
            }
        });

    static string ResolveLicense(string license)
    {
        if (!string.IsNullOrWhiteSpace(license))
        {
            return license.Trim();
        }

        var path = Path.Combine(AppContext.BaseDirectory, "license.txt");
        if (!File.Exists(path))
        {
            return "";
        }

        var text = File.ReadAllText(path).Trim();
        return text;
    }

    static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        var root = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var cpu = Path.Combine(root, "lib", "cpu");
        var dll = Path.Combine(cpu, "DocumentReaderSDK.dll");
        if (!File.Exists(dll))
        {
            throw new InvalidOperationException(
                "DocumentReaderSDK.dll is missing. Copy the Windows runtime into Platforms/Windows/native/lib/cpu/.");
        }

        if (!Native.SetDllDirectory(cpu) || Native.LoadLibrary(dll) == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Could not load DocumentReaderSDK.dll (" + Marshal.GetLastWin32Error() + ").");
        }

        Native.SetCurrentDirectory(root);
        _loaded = true;
    }

    static string Decode(byte[] buffer)
    {
        var length = Array.IndexOf(buffer, (byte)0);
        if (length < 0)
        {
            length = buffer.Length;
        }

        return Encoding.UTF8.GetString(buffer, 0, length);
    }

    static class Native
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr LoadLibrary(string path);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetDllDirectory(string path);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetCurrentDirectory(string path);

        [DllImport("DocumentReaderSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DocSDK_getMachineCode(byte[] machineCode);

        [DllImport("DocumentReaderSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DocSDK_activate(byte[] license);

        [DllImport("DocumentReaderSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DocSDK_initialize();

        [DllImport("DocumentReaderSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DocSDK_getLicenseStatus(byte[] output);

        [DllImport("DocumentReaderSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DocSDK_documentProcess(byte[] imagesJson, byte[] rfid, byte[] optionsJson, byte[] result);

        [DllImport("DocumentReaderSDK.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DocSDK_unload(byte[] result);
    }
}
