using BackendAPI.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace BackendAPI.Tests;

public sealed class AdminPipelineControllerTests
{
    [Fact]
    public void Controller_requires_existing_admin_policy()
    {
        var authorize=Assert.Single(typeof(AdminPipelineController).GetCustomAttributes(typeof(AuthorizeAttribute),true).Cast<AuthorizeAttribute>());
        Assert.Equal("Admin",authorize.Policy);
    }
}
