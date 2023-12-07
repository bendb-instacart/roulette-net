using Roulette.Protos;

namespace Roulette;

public class Fetcher : IFetcher
{
    private readonly IRouletteClient client;
    private readonly IRouletteApi api;

    private readonly SemaphoreSlim semaphore = new(1, 1);
    private string cursor = "";
    private DateTimeOffset lastFetchedAt = DateTimeOffset.MinValue;

    public Fetcher(IRouletteClient client, IRouletteApi api)
    {
        this.client = client;
        this.api = api;
    }

    public async Task<IReadOnlyList<IFeature>> FetchAsync(CancellationToken token)
    {
        await semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            return await FetchPaginatedFeaturesAsync(token).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<IReadOnlyList<IFeature>> FetchPaginatedFeaturesAsync(CancellationToken token)
    {
        var localCursor = cursor;

        Dictionary<string, Group> groupsByName = new Dictionary<string, Group>();
        List<IFeature> features = new List<IFeature>();

        while (true)
        {
            GetFeaturesRequest request = new()
            {
                Cursor = localCursor
            };

            GetFeaturesResponse response = await api.ListFeatures(request, token).ConfigureAwait(false);
            if (response.Cursor == localCursor)
            {
                // Implies that the response feature list is empty
                break;
            }

            localCursor = response.Cursor;

            if (response.Features.Count == 0)
            {
                // ...but we'll check just in case.
                break;
            }

            foreach (var groupProto in response.Groups)
            {
                if (groupsByName.ContainsKey(groupProto.Id))
                {
                    continue;
                }

                groupsByName[groupProto.Id] = Group.FromProto(groupProto);
            }

            foreach (var featureProto in response.Features)
            {
                var feature = Feature.FromProto(featureProto, client, groupsByName);
                if (feature != null)
                {
                    features.Add(feature);
                }
            }
        }

        this.cursor = localCursor;

        return features.AsReadOnly();
    }

    protected virtual void Dispose(bool disposing)
    {
        semaphore.Dispose();
        api.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
