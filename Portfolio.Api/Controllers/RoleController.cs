using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Application.Commands.Role.Create;
using Portfolio.Application.Commands.Role.Delete;
using Portfolio.Application.Commands.Role.Update;
using Portfolio.Application.Common.Responses;
using Portfolio.Application.DTOs;
using Portfolio.Application.Queries.Role;

namespace Portfolio.Api.Controllers;

/// <summary>
/// Handles role management operations.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
[Route("[controller]")]
[ApiController]
[Authorize(
    AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "Admin,Management")]
[EnableRateLimiting("ManagementPolicy")]
public class RoleController : ControllerBase
{
    private readonly IMediator _mediator;

    public RoleController(IMediator mediator) =>
        _mediator = mediator;

    [HttpPost("Create")]
    [ProducesDefaultResponseType(typeof(ApiResponse))]
    public async Task<ActionResult> CreateRoleAsync(RoleCreateCommand command)
    {
        await _mediator.Send(command);

        return Ok(ApiResponseFactory.Success("Role was created successfully.", HttpContext.TraceIdentifier));
    }

    [HttpGet("GetAll")]
    [ProducesDefaultResponseType(typeof(ApiResponse<IList<RoleResponseDTO>>))]
    public async Task<ActionResult<ApiResponse<IList<RoleResponseDTO>>>> GetRoleAsync()
    {
        var roleList = await _mediator.Send(new GetRoleQuery());

        return Ok(ApiResponseFactory.Success(roleList, "All roles retrieved successfully.", HttpContext.TraceIdentifier));
    }

    [HttpGet("{id}")]
    [ProducesDefaultResponseType(typeof(ApiResponse<RoleResponseDTO>))]
    public async Task<IActionResult> GetRoleByIdAsync(string id)
    {
        var role = await _mediator.Send(new GetRoleByIdQuery { RoleId = id });

        return Ok(ApiResponseFactory.Success(role, "Role returned successfully.", HttpContext.TraceIdentifier));
    }

    [HttpDelete("Delete/{id}")]
    [ProducesDefaultResponseType(typeof(ApiResponse))]
    public async Task<ActionResult<ApiResponse>> DeleteRoleAsync(string id)
    {
        await _mediator.Send(new DeleteRoleCommand { RoleId = id });

        return Ok(ApiResponseFactory.Success("Role deleted successfully.", HttpContext.TraceIdentifier));
    }

    [HttpPut("Edit/{id}")]
    [ProducesDefaultResponseType(typeof(ApiResponse))]
    public async Task<ActionResult<ApiResponse>> EditRole([FromBody] UpdateRoleCommand command)
    {
        await _mediator.Send(command);

        return Ok(ApiResponseFactory.Success("Role updated successfully.", HttpContext.TraceIdentifier));
    }
}