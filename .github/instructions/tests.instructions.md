---
applyTo:
  - 'backend/tests/**/*.cs'
---

# Test Coding Rules

## Test Framework

- **xUnit** for all tests
- Integration tests use `WebApplicationFactory<Program>`
- Test project: `backend/tests/HansOS.Api.IntegrationTests/`

## Naming Convention

- Format: `{Method}_{Scenario}_{ExpectedResult}`
- Examples:
  - `Login_WithValidCredentials_ReturnsAccessToken`
  - `GetMenuAll_Unauthorized_Returns401`
  - `UpdateProfile_WithInvalidEmail_Returns400`

## Coverage Requirements

Every new API endpoint must have at minimum:
1. **Happy path** — success scenario
2. **401 Unauthorized** — unauthenticated request
3. **400 Bad Request** — invalid parameters (if applicable)
4. **Business logic edge cases** — domain-specific scenarios

## Test Structure (AAA Pattern)

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/endpoint");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## Rules

- Bug fixes MUST include a test that reproduces the bug (red-green methodology)
- Do not use `Thread.Sleep` — use `Task.Delay` if needed
- Clean up test data in `Dispose` or use isolated test databases
- Prefer `FluentAssertions` for readable assertions
