// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Net;
using Duende.Bff.Configuration;
using Duende.Bff.Otel;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Duende.Bff.DynamicFrontends.Internal;

internal class IndexHtmlHttpClient : IIndexHtmlClient, IAsyncDisposable
{
    private readonly IOptions<BffOptions> _options;
    private readonly IHttpClientFactory _clientFactory;
    private readonly CurrentFrontendAccessor _currentFrontendAccessor;
    private readonly HybridCache _cache;
    private readonly ILogger<IndexHtmlHttpClient> _logger;
    private readonly IIndexHtmlTransformer? _transformer;
    private readonly CancellationTokenSource _stopping = new();

    public IndexHtmlHttpClient(IOptions<BffOptions> options,
        IHttpClientFactory clientFactory,
        CurrentFrontendAccessor currentFrontendAccessor,
        HybridCache cache,
        FrontendCollection frontendCollection,
        ILogger<IndexHtmlHttpClient> logger,
        IIndexHtmlTransformer? transformer = null)
    {
        _options = options;
        _clientFactory = clientFactory;
        _currentFrontendAccessor = currentFrontendAccessor;
        _cache = cache;
        _logger = logger;
        _transformer = transformer;
    }

    public async Task<string?> GetIndexHtmlAsync(CT ct = default)
    {
        var frontend = _currentFrontendAccessor.Get();

        var cacheKey = BuildCacheKey(frontend);

        try
        {
            return await _cache.GetOrCreateAsync(cacheKey, async (ct1) =>
                {
                    var client = _clientFactory.CreateClient(_options.Value.IndexHtmlClientName ??
                                                             Constants.HttpClientNames.IndexHtmlHttpClient);

                    var response = await client.GetAsync(frontend.IndexHtmlUrl, ct1);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        _logger.IndexHtmlRetrievalFailed(LogLevel.Information, frontend.Name,
                            response.StatusCode);
                        throw new PreventCacheException();
                    }

                    var html = await response.Content.ReadAsStringAsync(ct1);

                    if (_transformer == null)
                    {
                        return html;
                    }

                    _logger.RetrievedIndexHTML(LogLevel.Information, frontend.Name, response.StatusCode);

                    var transformed = await _transformer.Transform(html, ct1);
                    return transformed;
                },
                options: new HybridCacheEntryOptions()
                {
                    Expiration = TimeSpan.FromMinutes(5)
                },
                cancellationToken: ct);
        }
        catch (PreventCacheException)
        {
            return null;
        }
    }

    internal static string BuildCacheKey(BffFrontend frontend) => "Duende.Bff.IndexHtml:" + frontend.Name;

#pragma warning disable CA1032 // Do not use a custom message for this exception, as it is used to prevent caching
#pragma warning disable CA1064 // do not make this exception public as it's purely internal
    private class PreventCacheException : Exception
#pragma warning restore CA1064
#pragma warning restore CA1032
    {
    }

    public async ValueTask DisposeAsync()
    {
        await _stopping.CancelAsync();
        _stopping.Dispose();
    }
}
