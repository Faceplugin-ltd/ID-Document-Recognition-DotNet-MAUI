namespace DocumentReader.Net.Maui;

/// <summary>
/// Document Reader native SDK calls must not overlap (init + locate + recognize).
/// </summary>
static class SdkGate
{
    static readonly SemaphoreSlim Lock = new(1, 1);

    public static Task<T> RunAsync<T>(Func<Task<T>> work) => RunCoreAsync(work);

    public static Task RunAsync(Func<Task> work) =>
        RunCoreAsync(async () =>
        {
            await work().ConfigureAwait(false);
            return 0;
        });

    static async Task<T> RunCoreAsync<T>(Func<Task<T>> work)
    {
        await Lock.WaitAsync().ConfigureAwait(false);
        try
        {
            return await work().ConfigureAwait(false);
        }
        finally
        {
            Lock.Release();
        }
    }
}
