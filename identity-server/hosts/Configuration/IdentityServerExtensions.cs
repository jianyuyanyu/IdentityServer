// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Security.Cryptography.X509Certificates;
using Duende.IdentityServer;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Configuration.RequestProcessing;
using Duende.IdentityModel;
using IdentityServerHost.Configuration;
using IdentityServerHost.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace IdentityServerHost;

internal static class IdentityServerExtensions
{
    internal static WebApplicationBuilder ConfigureIdentityServer(this WebApplicationBuilder builder)
    {
        var identityServer = builder.Services.AddIdentityServer(options =>
            {
                options.Events.RaiseSuccessEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;

                options.EmitScopesAsSpaceDelimitedStringInJwt = true;
                options.Endpoints.EnableJwtRequestUri = true;

                options.ServerSideSessions.UserDisplayNameClaimType = JwtClaimTypes.Name;

                options.UserInteraction.CreateAccountUrl = "/Account/Create";
            })
            //.AddServerSideSessions()
            .AddInMemoryClients(new List<Duende.IdentityServer.Models.Client>())
            .AddInMemoryIdentityResources(Resources.IdentityResources)
            .AddInMemoryApiScopes(Resources.ApiScopes)
            .AddInMemoryApiResources(Resources.ApiResources)
            .AddExtensionGrantValidator<Extensions.ExtensionGrantValidator>()
            .AddExtensionGrantValidator<Extensions.NoSubjectExtensionGrantValidator>()
            .AddJwtBearerClientAuthentication()
            .AddAppAuthRedirectUriValidator()
            .AddTestUsers(TestUsers.Users)
            .AddProfileService<HostProfileService>()
            .AddCustomTokenRequestValidator<ParameterizedScopeTokenRequestValidator>()
            .AddScopeParser<ParameterizedScopeParser>()
            .AddMutualTlsSecretValidators()
            .AddInMemoryOidcProviders(new[]
            {
                new Duende.IdentityServer.Models.OidcProvider
                {
                    Scheme = "dynamicprovider-idsvr",
                    DisplayName = "IdentityServer (via Dynamic Providers)",
                    Authority = "https://demo.duendesoftware.com",
                    ClientId = "login",
                    ResponseType = "id_token",
                    Scope = "openid profile"
                }
            });

        builder.Services.AddIdentityServerConfiguration(opt =>
        {
            // opt.DynamicClientRegistration.SecretLifetime = TimeSpan.FromHours(1);
        }).AddInMemoryClientConfigurationStore();

        builder.Services.AddTransient<IDynamicClientRegistrationRequestProcessor, CustomClientRegistrationProcessor>();

        return builder;
    }
}