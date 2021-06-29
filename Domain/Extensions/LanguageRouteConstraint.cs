using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using vws.web.Enums;

namespace vws.web.Extensions
{
    public class LanguageRouteConstraint : IRouteConstraint
    {
        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (!values.ContainsKey("culture"))
                return false;

            var culture = values["culture"].ToString();

            foreach (var seedCulture in Enum.GetValues(typeof(SeedDataEnum.Cultures)))
                if (culture == seedCulture.ToString().Replace("_", "-"))
                    return true;

            return false;         
        }
    }
}
