using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Extensions;

public static class HttpContextAccessorExtensions
{

    public static bool TryGetUserId(this IHttpContextAccessor httpContextAccessor, out string? userId)
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst("sub");
        userId = userIdClaim?.Value;
        return !string.IsNullOrEmpty(userId);
    }

}
