namespace HansOS.Api.Models.ApiSpec;

/// <summary>API 規格總覽</summary>
public record ApiSpecResponse(
    string Title,
    string Version,
    List<EndpointSpec> Endpoints);

/// <summary>單一端點規格</summary>
public record EndpointSpec(
    string Route,
    string HttpMethod,
    string? Summary,
    string? RequestType,
    string? ResponseType);
