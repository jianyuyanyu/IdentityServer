// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Api.DPoP;

public class ConfigureJwtBearerOptions : IPostConfigureOptions<JwtBearerOptions>
{
    private readonly string _configScheme;

    public ConfigureJwtBearerOptions(string configScheme) => _configScheme = configScheme;

    public void PostConfigure(string name, JwtBearerOptions options)
    {
        if (_configScheme == name)
        {
            if (options.EventsType != null && !typeof(DPoPJwtBearerEvents).IsAssignableFrom(options.EventsType))
            {
                throw new InvalidOperationException("EventsType on JwtBearerOptions must derive from DPoPJwtBearerEvents to work with the DPoP support.");
            }
            if (options.Events != null && !typeof(DPoPJwtBearerEvents).IsAssignableFrom(options.Events.GetType()))
            {
                throw new InvalidOperationException("Events on JwtBearerOptions must derive from DPoPJwtBearerEvents to work with the DPoP support.");
            }

            if (options.Events == null && options.EventsType == null)
            {
                options.EventsType = typeof(DPoPJwtBearerEvents);
            }
        }
    }
}
