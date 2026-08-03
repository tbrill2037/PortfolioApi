using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Application.Commands.Email.ContactMe;
using Portfolio.Application.Common.Responses;

namespace Portfolio.Api.Controllers;

[Route("[controller]")]
[ApiController]
[EnableRateLimiting("EmailRequestPolicy")]
public class EmailController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmailController(IMediator mediator) =>
        _mediator = mediator;

    [HttpPost("ContactMe")]
    [ProducesDefaultResponseType(typeof(ApiResponse))]
    public async Task<ActionResult<ApiResponse>> QueueContactMeEmail([FromBody] QueueContactMeCommand command)
    {
        // Queue the request through the application layer rather than handling email delivery here.
        await _mediator.Send(command);

        return Ok(ApiResponseFactory.Success("Email was queued successfully.", HttpContext.TraceIdentifier));
    }

}