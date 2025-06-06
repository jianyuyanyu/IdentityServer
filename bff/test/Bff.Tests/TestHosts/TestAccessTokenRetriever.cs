// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.Bff.AccessTokenManagement;

namespace Duende.Bff.Tests.TestHosts;

public class TestAccessTokenRetriever : IAccessTokenRetriever
{
    public TestAccessTokenRetriever(Func<Task<AccessTokenResult>> accessTokenGetter) => _accessTokenGetter = accessTokenGetter;

    private readonly Func<Task<AccessTokenResult>> _accessTokenGetter;

    public async Task<AccessTokenResult> GetAccessTokenAsync(AccessTokenRetrievalContext context, CT ct = default) => await _accessTokenGetter();
}
