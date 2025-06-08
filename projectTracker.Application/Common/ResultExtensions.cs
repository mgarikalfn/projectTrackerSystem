using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace projectTracker.Application.Common
{
    public static class ResultExtensions
    {
        public static IActionResult ToActionResult<T>(this Result<T> result)
        {
            if (result.IsSuccess)
                return new OkObjectResult(new { success = true, data = result.Value });

            return new BadRequestObjectResult(new { success = false, errors = result.Errors.Select(e => e.Message) });
        }
    }

}
