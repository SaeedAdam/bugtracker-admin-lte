﻿using System.Security.Claims;
using System.Security.Principal;

namespace AdminLte.Mvc.Extensions;

public static class IdentityExtensions
{
    public static int? GetCompanyId(this IIdentity identity)
    {
        Claim claim = ((ClaimsIdentity)identity).FindFirst("CompanyId");
        return (claim is not null) ? int.Parse(claim.Value) : null;
    }

    public static string GetFullName(this IIdentity identity)
    {
        Claim claim = ((ClaimsIdentity)identity).FindFirst("FullName");
        return claim?.Value;
    }
}
