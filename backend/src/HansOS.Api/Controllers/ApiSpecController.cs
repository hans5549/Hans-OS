using HansOS.Api.Common;
using HansOS.Api.Models.ApiSpec;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace HansOS.Api.Controllers;

[ApiController]
[Route("api-spec")]
[AllowAnonymous]
public class ApiSpecController(IApiDescriptionGroupCollectionProvider apiExplorer) : ControllerBase
{
    /// <summary>取得所有 API 端點規格</summary>
    [HttpGet]
    public ApiEnvelope<ApiSpecResponse> GetSpec()
    {
        var endpoints = apiExplorer.ApiDescriptionGroups.Items
            .SelectMany(g => g.Items)
            .Where(d => d.ActionDescriptor is ControllerActionDescriptor cad
                && cad.ControllerTypeInfo.FullName?.StartsWith("HansOS.Api.Controllers") == true)
            .Select(d =>
            {
                var cad = (ControllerActionDescriptor)d.ActionDescriptor;
                var requestParam = d.ParameterDescriptions
                    .FirstOrDefault(p => p.Source.Id == "Body");
                var responseType = d.SupportedResponseTypes
                    .FirstOrDefault(r => r.StatusCode is 200 or 0);

                return new EndpointSpec(
                    Route: $"{d.HttpMethod} /{d.RelativePath}",
                    HttpMethod: d.HttpMethod ?? "UNKNOWN",
                    Summary: ExtractSummary(cad),
                    RequestType: requestParam?.Type?.Name,
                    ResponseType: FormatResponseType(responseType?.Type));
            })
            .OrderBy(e => e.Route)
            .ToList();

        return ApiEnvelope<ApiSpecResponse>.Success(
            new ApiSpecResponse("Hans-OS API", "v1", endpoints));
    }

    private static string? ExtractSummary(ControllerActionDescriptor cad)
    {
        var xmlDoc = cad.MethodInfo
            .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
            .Cast<System.ComponentModel.DescriptionAttribute>()
            .FirstOrDefault()?.Description;

        return xmlDoc ?? cad.ActionName;
    }

    private static string? FormatResponseType(Type? type)
    {
        if (type is null) return null;

        if (type.IsGenericType)
        {
            var genericName = type.Name.Split('`')[0];
            var args = string.Join(", ", type.GetGenericArguments().Select(FormatResponseType));
            return $"{genericName}<{args}>";
        }

        return type.Name;
    }
}
