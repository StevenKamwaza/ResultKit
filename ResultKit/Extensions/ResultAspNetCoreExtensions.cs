using ResultKit.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ResultKit.Extensions
{
    /// <summary>
    /// Extension methods that convert <see cref="Result"/> and <see cref="Result{T}"/>
    /// to ASP.NET Core <see cref="IActionResult"/> (MVC controllers)
    /// and <see cref="IResult"/> (Minimal APIs).
    /// </summary>
    public static class ResultAspNetCoreExtensions
    {
        //  IActionResult (MVC Controllers) 

        /// <summary>
        /// Converts a <see cref="Result{T}"/> to an <see cref="IActionResult"/>.
        /// Maps <see cref="ResultStatus"/> to the appropriate HTTP status code automatically.
        /// </summary>
        public static IActionResult ToActionResult<T>(this Result<T> result)
        {
            if (result.IsSuccess)
            {
                return result.Status switch
                {
                    ResultStatus.Created => new CreatedResult(string.Empty, result.Value),
                    ResultStatus.NoContent => new NoContentResult(),
                    _ => new OkObjectResult(result.Value)
                };
            }

            return result.Status.ToActionResult(result.Message, result.Errors);
        }

        /// <summary>
        /// Converts a <see cref="Result{T}"/> to an <see cref="IActionResult"/>,
        /// applying a custom projection to the success value.
        /// </summary>
        public static IActionResult ToActionResult<T, TResponse>(
            this Result<T> result,
            Func<T, TResponse> projection)
        {
            if (result.IsSuccess)
                return new OkObjectResult(projection(result.Value!));

            return result.Status.ToActionResult(result.Message, result.Errors);
        }

        /// <summary>
        /// Converts a non-generic <see cref="Result"/> to an <see cref="IActionResult"/>.
        /// </summary>
        public static IActionResult ToActionResult(this Result result)
        {
            if (result.IsSuccess)
            {
                return result.Status switch
                {
                    ResultStatus.Created => new StatusCodeResult(StatusCodes.Status201Created),
                    ResultStatus.NoContent => new NoContentResult(),
                    _ => string.IsNullOrEmpty(result.Message)
                                                  ? new OkResult()
                                                  : new OkObjectResult(new { result.Message })
                };
            }

            return result.Status.ToActionResult(result.Message, result.Errors);
        }

        //  IResult (Minimal APIs) 

        /// <summary>
        /// Converts a <see cref="Result{T}"/> to an <see cref="IResult"/> for use in Minimal APIs.
        /// </summary>
        public static IResult ToHttpResult<T>(this Result<T> result)
        {
            if (result.IsSuccess)
            {
                return result.Status switch
                {
                    ResultStatus.Created => Results.Created(string.Empty, result.Value),
                    ResultStatus.NoContent => Results.NoContent(),
                    _ => Results.Ok(result.Value)
                };
            }

            return result.Status.ToHttpResult(result.Message, result.Errors);
        }

        /// <summary>
        /// Converts a non-generic <see cref="Result"/> to an <see cref="IResult"/> for use in Minimal APIs.
        /// </summary>
        public static IResult ToHttpResult(this Result result)
        {
            if (result.IsSuccess)
            {
                return result.Status switch
                {
                    ResultStatus.Created => Results.StatusCode(StatusCodes.Status201Created),
                    ResultStatus.NoContent => Results.NoContent(),
                    _ => string.IsNullOrEmpty(result.Message)
                                                  ? Results.Ok()
                                                  : Results.Ok(new { result.Message })
                };
            }

            return result.Status.ToHttpResult(result.Message, result.Errors);
        }

        //  Problem Details support 

        /// <summary>
        /// Converts a failed <see cref="Result{T}"/> to an RFC 7807 <see cref="ProblemDetails"/> response.
        /// </summary>
        public static IActionResult ToProblemResult<T>(this Result<T> result, string? instance = null)
        {
            if (result.IsSuccess)
                return new OkObjectResult(result.Value);

            var problem = BuildProblemDetails(result.Status, result.Message, result.Errors, instance);
            return new ObjectResult(problem) { StatusCode = problem.Status };
        }

        //  Internal helpers 

        internal static IActionResult ToActionResult(
            this ResultStatus status,
            string message,
            IReadOnlyList<ErrorDetail> errors)
        {
            var body = BuildErrorBody(message, errors);
            return new ObjectResult(body) { StatusCode = status.ToHttpStatusCode() };
        }

        internal static IResult ToHttpResult(
            this ResultStatus status,
            string message,
            IReadOnlyList<ErrorDetail> errors)
        {
            var body = BuildErrorBody(message, errors);
            return Results.Json(body, statusCode: status.ToHttpStatusCode());
        }

        private static ProblemDetails BuildProblemDetails(
            ResultStatus status,
            string message,
            IReadOnlyList<ErrorDetail> errors,
            string? instance)
        {
            var problem = new ProblemDetails
            {
                Status = status.ToHttpStatusCode(),
                Title = status.ToTitle(),
                Detail = message,
                Instance = instance
            };

            if (errors.Count > 0)
            {
                problem.Extensions["errors"] = errors.Select(e => new
                {
                    e.Code,
                    e.Message,
                    e.Field
                });
            }

            return problem;
        }

        private static object BuildErrorBody(string message, IReadOnlyList<ErrorDetail> errors)
        {
            if (errors.Count == 0)
                return new { Message = message };

            return new
            {
                Message = message,
                Errors = errors.Select(e => new { e.Code, e.Message, e.Field })
            };
        }
    }

    /// <summary>
    /// Extension methods on <see cref="ResultStatus"/> for HTTP conversions.
    /// </summary>
    public static class ResultStatusHttpExtensions
    {
        /// <summary>Maps a <see cref="ResultStatus"/> to its corresponding HTTP status code integer.</summary>
        public static int ToHttpStatusCode(this ResultStatus status) => status switch
        {
            ResultStatus.Ok => StatusCodes.Status200OK,
            ResultStatus.Created => StatusCodes.Status201Created,
            ResultStatus.NoContent => StatusCodes.Status204NoContent,
            ResultStatus.Invalid => StatusCodes.Status400BadRequest,
            ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ResultStatus.NotFound => StatusCodes.Status404NotFound,
            ResultStatus.Conflict => StatusCodes.Status409Conflict,
            ResultStatus.UnprocessableEntity => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };

        /// <summary>Returns a short human-readable title for a status.</summary>
        public static string ToTitle(this ResultStatus status) => status switch
        {
            ResultStatus.Ok => "Success",
            ResultStatus.Created => "Created",
            ResultStatus.NoContent => "No Content",
            ResultStatus.Invalid => "Bad Request",
            ResultStatus.Unauthorized => "Unauthorized",
            ResultStatus.Forbidden => "Forbidden",
            ResultStatus.NotFound => "Not Found",
            ResultStatus.Conflict => "Conflict",
            ResultStatus.UnprocessableEntity => "Unprocessable Entity",
            _ => "Internal Server Error"
        };
    }

}
