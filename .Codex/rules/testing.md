# Testing Rules

Use this for backend tests under `backend/tests/*` and for any code change that affects API or service behavior.

## Test Projects

| Project | Purpose |
|---------|---------|
| `backend/tests/HansOS.Api.UnitTests` | Service, model, seeding, and DbContext unit tests |
| `backend/tests/HansOS.Api.IntegrationTests` | HTTP endpoint and pipeline integration tests |

Integration tests use `HansOsWebApplicationFactory`, `WebApplicationFactory<Program>`, EF InMemory, and test JWT overrides.

## Naming

Test method format:

```text
{Method}_{Scenario}_{ExpectedResult}
```

Examples:

- `Login_WithValidCredentials_ReturnsAccessToken`
- `GetMenuAll_Unauthorized_Returns401`
- `UpdateProfile_WithInvalidEmail_Returns400`

## Required Coverage

Every new API endpoint must cover:

1. Happy path
2. `401 Unauthorized`
3. `400 Bad Request`, if applicable
4. Business logic edge cases

Every new public service method must have a unit or integration test.

Bug fixes must include a test that reproduces the bug before the fix.

## Structure

Use Arrange / Act / Assert:

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/endpoint");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## Assertions

- Prefer `FluentAssertions` for readable assertions.
- Assert status codes and response body shape for endpoint tests.
- For `ApiEnvelope<T>` endpoints, assert `code`, `data`, and `error/message` expectations.
- For auth tests, assert cookie/token behavior when relevant.

## Data Isolation

- Prefer isolated in-memory databases per integration test factory instance.
- Avoid ordering dependencies between tests.
- Do not use `Thread.Sleep`; use deterministic waits or `Task.Delay` only when unavoidable.
- Keep seed assumptions explicit.

## Verification Commands

```bash
dotnet test backend/HansOS.slnx
```

For narrow work, run targeted tests first, then broaden to the solution test command when risk justifies it.
