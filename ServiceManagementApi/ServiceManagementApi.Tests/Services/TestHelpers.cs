using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Generic;
using System.Linq;

namespace ServiceManagementApi.Tests.Services;

// Helper to create a mock UserManager for service tests
public static class TestHelpers
{
    public static Mock<UserManager<TUser>> MockUserManager<TUser>(List<TUser>? users = null) where TUser : class
    {
        var store = new Mock<IUserStore<TUser>>();
        var mgr = new Mock<UserManager<TUser>>(
            store.Object,
            new Mock<IOptions<IdentityOptions>>().Object,
            new Mock<IPasswordHasher<TUser>>().Object,
            new IUserValidator<TUser>[0],
            new IPasswordValidator<TUser>[0],
            new Mock<ILookupNormalizer>().Object,
            new IdentityErrorDescriber(),
            null,
            new Mock<ILogger<UserManager<TUser>>>().Object);

        users ??= new List<TUser>();

        mgr.Setup(x => x.Users).Returns(users.AsQueryable());
        return mgr;
    }
}

