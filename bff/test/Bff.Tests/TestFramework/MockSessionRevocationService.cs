// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.Bff.SessionManagement.Revocation;
using Duende.Bff.SessionManagement.SessionStore;

namespace Duende.Bff.Tests.TestFramework;

public class MockSessionRevocationService : ISessionRevocationService
{
    public bool DeleteUserSessionsWasCalled { get; set; }
    public UserSessionsFilter? DeleteUserSessionsFilter { get; set; }
    public Task RevokeSessionsAsync(UserSessionsFilter filter, CT ct)
    {
        DeleteUserSessionsWasCalled = true;
        DeleteUserSessionsFilter = filter;
        return Task.CompletedTask;
    }
}
