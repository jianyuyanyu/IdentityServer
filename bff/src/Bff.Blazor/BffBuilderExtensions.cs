// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.Bff.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Duende.Bff.Blazor;

public static class
    BffBuilderExtensions
{
    public static T AddBlazorServer<T>(this T builder, Action<BffBlazorServerOptions>? configureOptions = null) where T : IBffServicesBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Todo: EV: make sure server side sessions is added, as it doesn't work otherwise. 
        builder.Services
            .AddOpenIdConnectAccessTokenManagement()
            .AddBlazorServerAccessTokenManagement<ServerSideTokenStore>()
            .AddSingleton<IClaimsTransformation, AddServerManagementClaimsTransform>()
            .AddScoped<AuthenticationStateProvider, BffServerAuthenticationStateProvider>();


        if (configureOptions != null)
        {
            builder.Services.Configure(configureOptions);
        }

        return builder;
    }
}
