using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Portfolio.Application.Commands.User.Create;
using Portfolio.Application.Commands.User.Delete;
using Portfolio.Application.Commands.User.Update.AssignUsersRole;
using Portfolio.Application.Commands.User.Update.EditUserProfile;
using Portfolio.Application.Commands.User.Update.UpdateUserRoles;
using Portfolio.Application.Common.Responses;
using Portfolio.Application.DTOs;
using Portfolio.Application.Queries.User;

namespace Portfolio.Api.Controllers;

/// <summary>
/// Handles user management operations.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
[Route("[controller]")]
[ApiController]
[EnableRateLimiting("ManagementPolicy")]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator) =>
        _mediator = mediator;

    [HttpPost("Create")]
    [ProducesDefaultResponseType(typeof(ApiResponse))]
    public async Task<ActionResult<ApiResponse>> CreateUser(CreateUserCommand command)
    {
        await _mediator.Send(command);

        return Ok(ApiResponseFactory.Success("User was created successfully.", HttpContext.TraceIdentifier));
    }

    [HttpGet("GetAll")]
    [ProducesDefaultResponseType(typeof(ApiResponse<List<UserResponseDTO>>))]
    public async Task<ActionResult<List<UserResponseDTO>>> GetAllUserAsync()
    {
        var userList = await _mediator.Send(new GetUserQuery());

        return Ok(ApiResponseFactory.Success(userList, "All users retrieved successfully.", HttpContext.TraceIdentifier));
    }

    [HttpDelete("Delete/{userId}")]
    [ProducesDefaultResponseType(typeof(ApiResponse))]
    public async Task<ActionResult<ApiResponse>> DeleteUser(string userId)
    {
        await _mediator.Send(new DeleteUserCommand { Id = userId });

        return Ok(ApiResponseFactory.Success("User deleted successfully.", HttpContext.TraceIdentifier));
    }

    [HttpGet("GetUserDetails/{userId}")]
    [ProducesDefaultResponseType(typeof(ApiResponse<UserDetailsResponseDTO>))]
    public async Task<ActionResult<UserDetailsResponseDTO>> GetUserDetails(string userId)
    {
        var userDetails = await _mediator.Send(new GetUserDetailsQuery { UserId = userId });

        return Ok(ApiResponseFactory.Success(userDetails, "User details retrieved successfully.", HttpContext.TraceIdentifier));
    }

    [HttpGet("GetUserDetailsByUserName/{userName}")]
    [ProducesDefaultResponseType(typeof(ApiResponse<UserDetailsResponseDTO>))]
    public async Task<ActionResult<UserDetailsResponseDTO>> GetUserDetailsByUserName(string userName)
    {
        var userDetails = await _mediator.Send(new GetUserDetailsByUserNameQuery { UserName = userName });

        return Ok(ApiResponseFactory.Success(userDetails, "User details retrieved successfully.", HttpContext.TraceIdentifier));
    }

    [HttpPost("AssignRoles")]
    [ProducesDefaultResponseType(typeof(ApiResponse))]
    public async Task<ActionResult<ApiResponse>> AssignRoles(AssignUsersRoleCommand command)
    {
        await _mediator.Send(command);

        return Ok(ApiResponseFactory.Success("Role assigned successfully.", HttpContext.TraceIdentifier));
    }

    [HttpPut("EditUserRoles")]
    [ProducesDefaultResponseType(typeof(ApiResponse))]
    public async Task<ActionResult<ApiResponse>> EditUserRoles(UpdateUserRolesCommand command)
    {
        await _mediator.Send(command);

        return Ok(ApiResponseFactory.Success("User role edited successfully.", HttpContext.TraceIdentifier));
    }

    [HttpGet("GetAllUserDetails")]
    [ProducesDefaultResponseType(typeof(ApiResponse<UserDetailsResponseDTO>))]
    public async Task<ActionResult<ApiResponse<UserDetailsResponseDTO>>> GetAllUserDetails()
    {
        var userDetailsList = await _mediator.Send(new GetAllUsersDetailsQuery());

        return Ok(ApiResponseFactory.Success(userDetailsList, "All user details retrieved successfully.", HttpContext.TraceIdentifier));
    }

    [HttpPut("EditUserProfile/{id}")]
    [ProducesDefaultResponseType(typeof(ApiResponse))]
    public async Task<ActionResult<ApiResponse>> EditUserProfile([FromBody] EditUserProfileCommand command)
    {
        await _mediator.Send(command);

        return Ok(ApiResponseFactory.Success("User profile edited successfully.", HttpContext.TraceIdentifier));
    }
}