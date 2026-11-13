using System.Runtime.InteropServices;
using CoreFoundation;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace DocumentReader.Net.Maui;

public static partial class DocumentReaderSdk
{
    public static partial Task<string> GetMachineCodeAsync() =>
        Task.Run(() => StringSend("getMachineCode"));

    public static partial Task<int> SetActivationAsync(string license) =>
        Task.Run(() =>
        {
            using var key = new NSString(license);
            return int_objc_msgSend_IntPtr(DocClass(), Selector.GetHandle("setActivation:"), key.Handle);
        });

    public static partial Task<int> InitAsync() =>
        Task.Run(() => int_objc_msgSend(DocClass(), Selector.GetHandle("initSDK")));

    public static partial Task<string> GetLicenseStatusAsync() =>
        Task.Run(() => StringSend("getLicenseStatus"));

    public static partial Task<string> LastLicenseErrorAsync() =>
        Task.Run(() => StringSend("lastLicenseError"));

    public static partial Task<string> LocateDocumentAsync(byte[] jpeg) =>
        Task.Run(() =>
        {
            using var data = NSData.FromArray(jpeg);
            using var image = UIImage.LoadFromData(data)
                ?? throw new InvalidOperationException("Could not decode camera frame.");
            var result = IntPtr_objc_msgSend_IntPtr(DocClass(), Selector.GetHandle("locateDocument:"), image.Handle);
            return ReadString(result);
        });

    public static partial Task<string> RecognizeAsync(byte[] frontJpeg, byte[]? backJpeg, string authenticityMode) =>
        Task.Run(() =>
        {
            using var options = new NSString("{\"scenario\":\"FullProcess\",\"series\":false}");
            IntPtr_objc_msgSend_IntPtr(DocClass(), Selector.GetHandle("startNewSession:"), options.Handle);

            using var frontData = NSData.FromArray(frontJpeg);
            using var front = UIImage.LoadFromData(frontData)
                ?? throw new InvalidOperationException("Could not decode the front image.");
            using var mode = new NSString(authenticityMode);
            UIImage? back = null;
            NSData? backData = null;
            if (backJpeg is { Length: > 0 })
            {
                backData = NSData.FromArray(backJpeg);
                back = UIImage.LoadFromData(backData);
            }

            try
            {
                var result = IntPtr_objc_msgSend_IntPtr_IntPtr_IntPtr(
                    DocClass(),
                    Selector.GetHandle("recognizeFront:back:authenticityMode:"),
                    front.Handle,
                    back?.Handle ?? IntPtr.Zero,
                    mode.Handle);
                return ReadString(result);
            }
            finally
            {
                back?.Dispose();
                backData?.Dispose();
            }
        });

    public static partial Task DeinitAsync() =>
        Task.Run(() =>
        {
            IntPtr_objc_msgSend(DocClass(), Selector.GetHandle("deinitSDK"));
        });

    static IntPtr DocClass()
    {
        var handle = Class.GetHandle("DocSDK");
        if (handle == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "docsdk.framework is not loaded. Copy it to Platforms/iOS/Frameworks/.");
        }

        return handle;
    }

    static string StringSend(string selector) =>
        ReadString(IntPtr_objc_msgSend(DocClass(), Selector.GetHandle(selector)));

    static string ReadString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
        {
            return "";
        }

        return CFString.FromHandle(ptr) ?? "";
    }

    [DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    static extern IntPtr IntPtr_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    static extern int int_objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    static extern int int_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    static extern IntPtr IntPtr_objc_msgSend_IntPtr_IntPtr_IntPtr(
        IntPtr receiver,
        IntPtr selector,
        IntPtr arg1,
        IntPtr arg2,
        IntPtr arg3);
}
