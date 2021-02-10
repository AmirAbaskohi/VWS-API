using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using vws.web.Core;

namespace ActionFilters.ActionFilters
{
    public class TokenValidationFilterAttribute : IActionFilter
    {
        public static HashSet<UserToken> Tokens = new HashSet<UserToken>();

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var token = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {

        }
    }
}