using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Xunit;
using Xunit.Abstractions;

namespace Roulette.Tests;

public class HttpApiTest
{
    private readonly ITestOutputHelper output;

    public HttpApiTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public async Task TestName()
    {
        RequestDelegate requestHandler = context =>
        {
            Protos.GetFeaturesResponse resp = new();
            resp.Cursor = "halooooo";
            resp.Features.Add(new Protos.Feature());

            context.Response.ContentType = "application/protobuf";

            using (CodedOutputStream cs = new(context.Response.Body, leaveOpen: true))
            {
                resp.WriteTo(cs);
            }

            return Task.CompletedTask;
        };

        IWebHost host = new WebHostBuilder()
            .UseUrls("http://127.0.0.1:0") // "0" means "any open port"
            .UseKestrel()
            .UseContentRoot(Environment.CurrentDirectory)
            .Configure(app => app.Run(requestHandler))
            .Build();

        host.Start();

        try
        {
            IServerAddressesFeature addresses = host.ServerFeatures.Get<IServerAddressesFeature>();
            string baseAddr = addresses.Addresses.First();
            
            HttpClient client = new() { BaseAddress = new Uri(baseAddr) };
            HttpRouletteApi api = new(client);
            CancellationTokenSource cts = new();

            var resp = await api.ListFeatures(new(), cts.Token);

            Assert.NotEmpty(resp.Features);
            Assert.Equal("halooooo", resp.Cursor);
        }
        finally
        {
            host.Dispose();
        }
    }
}