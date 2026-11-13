namespace DocumentReader.Net.Maui;

public sealed class LocateSession : IDisposable
{
    const int ShowThreshold = 50;
    const int HighThreshold = 85;
    const int KeepCaptureMin = 50;
    const int PollMs = 450;

    readonly Func<Task<byte[]?>> _capture;
    readonly Action<LocateFrame> _onFrame;
    readonly System.Timers.Timer _timer;
    int _locating;
    bool _stopped = true;
    double _viewW;
    double _viewH;
    long _lastStillMs;
    (double W, double H)? _previewSize;

    public LocateSession(Func<Task<byte[]?>> capture, Action<LocateFrame> onFrame)
    {
        _capture = capture;
        _onFrame = onFrame;
        _timer = new System.Timers.Timer(PollMs);
        _timer.Elapsed += (_, _) => ScheduleTick();
    }

    public byte[]? LatestCapture { get; private set; }

    public void UpdateViewSize(double width, double height)
    {
        _viewW = width;
        _viewH = height;
    }

    public void SetPreviewSize(double width, double height) => _previewSize = (width, height);

    public void Start()
    {
        _stopped = false;
        _timer.Start();
    }

    public void Stop()
    {
        _stopped = true;
        _timer.Stop();
    }

    public void Dispose()
    {
        Stop();
        _timer.Dispose();
    }

    void ScheduleTick()
    {
        if (_stopped || Volatile.Read(ref _locating) == 1)
        {
            return;
        }

        // Snapshot must run on the UI thread, but a background thread must not wait for it.
        // SaveSnapShot re-enters the UI thread; awaiting it from a worker deadlocks Android.
        MainThread.BeginInvokeOnMainThread(() => _ = TickAsync());
    }

    async Task TickAsync()
    {
        if (_stopped || Interlocked.CompareExchange(ref _locating, 1, 0) == 1)
        {
            return;
        }

        try
        {
            var jpeg = await _capture().ConfigureAwait(true);
            if (jpeg == null || _stopped)
            {
                return;
            }

            var locateJson = await Task.Run(() => DocumentReaderSdk.LocateDocumentAsync(jpeg)).ConfigureAwait(true);
            var pct = ResultView.DocumentPercent(locateJson);
            var pts = ResultView.DocumentCorners(locateJson);
            var show = pct >= ShowThreshold && pts != null;
            var high = pct >= HighThreshold;
            var imageSize = ResultView.LocateImageSize(locateJson);
            if (imageSize == null && _previewSize is { } preview)
            {
                imageSize = ResultView.UprightSnapshotSize(preview.W, preview.H);
            }

            IReadOnlyList<DocPoint>? mapped = null;
            if (pts != null && show && imageSize != null && _viewW > 0 && _viewH > 0)
            {
                mapped = ResultView.MapUprightCornersToView(
                    pts,
                    imageSize.Value.Width,
                    imageSize.Value.Height,
                    _viewW,
                    _viewH);
            }

            if (pct >= KeepCaptureMin)
            {
                var now = Environment.TickCount64;
                if (high || now - _lastStillMs > 500)
                {
                    LatestCapture = jpeg;
                    _lastStillMs = now;
                }
            }

            var frame = new LocateFrame(pct, mapped, high, show);
            MainThread.BeginInvokeOnMainThread(() => _onFrame(frame));
        }
        catch
        {
            // Skip bad frames — same as Flutter/RN locate loop.
        }
        finally
        {
            Interlocked.Exchange(ref _locating, 0);
        }
    }
}

public readonly record struct LocateFrame(
    int ScorePct,
    IReadOnlyList<DocPoint>? Corners,
    bool High,
    bool Show);
