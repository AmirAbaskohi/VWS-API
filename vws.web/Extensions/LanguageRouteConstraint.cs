using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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
            return ( culture == SeedDataEnum.Cultures.en_US.ToString().Replace("_", "-") ||
                     culture == SeedDataEnum.Cultures.fr_FR.ToString().Replace("_", "-") ||
                     culture == SeedDataEnum.Cultures.ru_RU.ToString().Replace("_", "-") ||
                     culture == SeedDataEnum.Cultures.es_SP.ToString().Replace("_", "-") ||
                     culture == SeedDataEnum.Cultures.pt_PG.ToString().Replace("_", "-") ||
                     culture == SeedDataEnum.Cultures.fa_IR.ToString().Replace("_", "-") ||
                     culture == SeedDataEnum.Cultures.ar_SB.ToString().Replace("_", "-") ||
                     culture == SeedDataEnum.Cultures.de_GE.ToString().Replace("_", "-") ||
                     culture == SeedDataEnum.Cultures.it_IT.ToString().Replace("_", "-") ||
                     culture == SeedDataEnum.Cultures.tr_TU.ToString().Replace("_", "-")
                );            
        }
    }
}
