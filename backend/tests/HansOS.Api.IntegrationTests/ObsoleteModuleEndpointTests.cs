using System.Net;
using FluentAssertions;

namespace HansOS.Api.IntegrationTests;

public class ObsoleteModuleEndpointTests(HansOsWebApplicationFactory factory)
    : IClassFixture<HansOsWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/todo/items")]
    [InlineData("/todo/projects")]
    [InlineData("/todo/categories")]
    [InlineData("/todo/tags")]
    [InlineData("/article-collection/bookmarks")]
    [InlineData("/article-collection/groups")]
    public async Task RemovedModuleEndpoints_ReturnNotFound(string path)
    {
        var response = await _client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
