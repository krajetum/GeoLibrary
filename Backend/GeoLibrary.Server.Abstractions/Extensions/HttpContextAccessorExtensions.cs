using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Extensions;

public static class HttpContextAccessorExtensions
{

    public static bool TryGetUserId(this IHttpContextAccessor httpContextAccessor, out Guid userId)
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst("sub");

        if(Guid.TryParse(userIdClaim?.Value, out var userIdGuid))
        {
            userId = userIdGuid;
        }
        else
        {
            userId = Guid.Empty;
        }

        return userId != Guid.Empty;
    }

    public static bool TryGetUsername(this IHttpContextAccessor httpContextAccessor, out string username)
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst("name");
        username = userIdClaim?.Value ?? string.Empty;
        return !string.IsNullOrEmpty(username);
    }

    public static bool TryGetEmail(this IHttpContextAccessor httpContextAccessor, out string email)
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst("email");
        email = userIdClaim?.Value ?? string.Empty;
        return !string.IsNullOrEmpty(email);
    }

    public static bool TryGetUserSignature(this IHttpContextAccessor httpContextAccessor, out string userSignature)
    {
        if(httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("X-User-Signature", out var signatureValue) == true)
        {

            if (signatureValue.Count == 0)
            {
                userSignature = string.Empty;
                return false;
            }
            userSignature = signatureValue[0] ?? string.Empty;
            return string.IsNullOrWhiteSpace(userSignature) == false;
        }
        else
        {
            userSignature = string.Empty;
            return false;
        }
    }

}
