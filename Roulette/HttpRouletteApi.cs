using Roulette.Protos;

namespace Roulette;

public class HttpRouletteApi : IRouletteApi
{
    private static readonly Uri ListFeaturesUri = new Uri(
        "rpc/instacart.roulette.v1.RouletteService/ListFeatures",
        UriKind.Relative);

    private readonly HttpClient httpClient;

    public HttpRouletteApi(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<GetFeaturesResponse> ListFeatures(GetFeaturesRequest request, CancellationToken token)
    {
        using var ms = new MemoryStream(request.CalculateSize());
        using (var cs = new Google.Protobuf.CodedOutputStream(ms, leaveOpen: true))
        {
            request.WriteTo(cs);
            cs.Flush();
        }
        ms.Seek(0, SeekOrigin.Begin);

        using StreamContent content = new(ms);
        content.Headers.Add("Content-Type", "application/protobuf");

        using var rpcRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = ListFeaturesUri,
            Content = content,
        };

        using var rpcResponse = await httpClient.SendAsync(rpcRequest, token).ConfigureAwait(false);
        rpcResponse.EnsureSuccessStatusCode();

        var responseBytes = await rpcResponse.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
        if (responseBytes == null)
        {
            throw new HttpRequestException("ListFeatures response contained no body");
        }

        using (var cs = new Google.Protobuf.CodedInputStream(responseBytes))
        {
            var response = new GetFeaturesResponse();
            response.MergeFrom(cs);

            return response;
        }
    }

    public void Dispose() => httpClient.Dispose();
}
