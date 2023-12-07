using Roulette.Protos;

namespace Roulette;

/// <summary>
/// Represents an object that can communicate with the Roulette RPC server.
/// </summary>
public interface IRouletteApi : IDisposable
{
    Task<GetFeaturesResponse> ListFeatures(GetFeaturesRequest request, CancellationToken token);
}
