namespace HansOS.Api.Common;

public sealed class ForbiddenException(string message) : Exception(message);
