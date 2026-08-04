using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Application.Commands.Email.ContactMe;
using Portfolio.Application.Common.Responses;

namespace Portfolio.Api.Controllers;

[Route("[controller]")]
[ApiController]
[EnableRateLimiting("EmailRequestPolicy")]
[Produces("application/json")]
public sealed class EmailController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmailController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Queues a portfolio contact message for delivery.
    /// </summary>
    /// <remarks>
    /// Validates the submitted name, email address, and message,
    /// then persists the request for asynchronous email processing.
    /// </remarks>
    /// <param name="command">
    /// The contact message submitted by a portfolio visitor.
    /// </param>
    /// <returns>
    /// A response indicating that the message was successfully queued.
    /// </returns>
    [HttpPost("ContactMe")]
    [Consumes("application/json")]
    [ProducesResponseType(
        typeof(ApiResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse),
        StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(
        typeof(ApiResponse),
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse>>
        QueueContactMeEmail(
            [FromBody] QueueContactMeCommand command)
    {
        await _mediator.Send(command);

        return Ok(
            ApiResponseFactory.Success(
                "Email was queued successfully.",
                HttpContext.TraceIdentifier));
    }
}