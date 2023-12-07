namespace Roulette;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// An IFetcher is a thing that knows how to fetch features and groups
/// from the Roulette backend.
/// </summary>
public interface IFetcher : IDisposable
{
    Task<IReadOnlyList<IFeature>> FetchAsync(CancellationToken token);
}
