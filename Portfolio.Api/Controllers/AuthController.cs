using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Application.Commands.Auth;
using Portfolio.Application.Common.Responses;

namespace Portfolio.Api.Controllers;

/// <summary>
/// Handles authentication requests.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
[Route("[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[EnableRateLimiting("AuthPolicy")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) =>
        _mediator = mediator;

    [HttpPost("Login")]
    [ProducesDefaultResponseType(typeof(ApiResponse))]
    public async Task<ActionResult<ApiResponse>> Login([FromBody] AuthCommand command)
    {
        await _mediator.Send(command);

        return Ok(ApiResponseFactory.Success("User login was successful.", HttpContext.TraceIdentifier));
    }
}