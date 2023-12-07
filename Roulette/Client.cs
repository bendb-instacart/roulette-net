namespace Roulette;

public class Client : IDisposable
{
    private readonly IFetcher fetcher;
    private readonly CancellationTokenSource cts = new CancellationTokenSource();
    private readonly ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
    private readonly Dictionary<string, IFeature> featuresByName = new();
    private readonly Dictionary<Guid, IFeature> featuresByGuid = new();

    private Task? fetcherTask = null;

    public Client(IFetcher fetcher)
    {
        this.fetcher = fetcher;
    }

    public IFeature? GetFeatureByName(string name)
    {
        return rwLock.Read(() =>
        {
            IFeature? value;
            featuresByName.TryGetValue(name, out value);
            return value;
        });
    }

    public IFeature? this[string name] => GetFeatureByName(name);

    public IFeature? GetFeatureByGuid(Guid guid)
    {
        return rwLock.Read(() =>
        {
            IFeature? value;
            featuresByGuid.TryGetValue(guid, out value);
            return value;
        });
    }

    public IFeature? this[Guid guid] => GetFeatureByGuid(guid);

    public void Start()
    {
        rwLock.Write(() =>
        {
            if (fetcherTask != null)
            {
                return;
            }

            fetcherTask = Task.Run(FetchInBackground, cts.Token);
        });
    }

    public void Stop()
    {
        rwLock.Write(() =>
        {
            if (fetcherTask == null)
            {
                return;
            }

            cts.Cancel();
            fetcherTask = null;
        });
    }

    private async Task FetchInBackground()
    {
        while (!cts.IsCancellationRequested)
        {
            IReadOnlyList<IFeature> features = await fetcher.FetchAsync(cts.Token);

            rwLock.Write(() =>
            {
                foreach (IFeature feature in features)
                {
                    featuresByName[feature.Name] = feature;
                    featuresByGuid[feature.Guid] = feature;
                }
            });

            await Task.Delay(TimeSpan.FromSeconds(30), cts.Token);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        cts.Cancel();
        fetcherTask = null;
        fetcher.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
