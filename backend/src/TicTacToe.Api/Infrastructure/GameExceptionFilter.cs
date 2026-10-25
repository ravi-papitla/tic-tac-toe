using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Infrastructure;

/// <summary>
/// Maps domain exceptions to RFC 7807 problem responses:
/// 404 unknown game, 400 move outside the board, 409 any other rule violation.
/// The "code" extension carries the <see cref="GameErrorCode"/> for programmatic handling.
/// </summary>
public sealed class GameExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        ProblemDetails problem;

        switch (context.Exception)
        {
            case GameNotFoundException notFound:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Game not found",
                    Detail = notFound.Message
                };
                problem.Extensions["code"] = "GameNotFound";
                break;

            case GameRuleException rule:
                problem = new ProblemDetails
                {
                    Status = rule.Code == GameErrorCode.OutOfBounds
                        ? StatusCodes.Status400BadRequest
                        : StatusCodes.Status409Conflict,
                    Title = "Invalid operation",
                    Detail = rule.Message
                };
                problem.Extensions["code"] = rule.Code.ToString();
                break;

            default:
                return; // anything else is a real bug: let the framework return a 500
        }

        context.Result = new ObjectResult(problem) { StatusCode = problem.Status };
        context.ExceptionHandled = true;
    }
}
