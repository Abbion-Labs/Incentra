using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VariableCompensation.Api.Contracts.Auth;
using VariableCompensation.Application.Auth.Commands.UpdateAdminUser;
using VariableCompensation.Application.Auth.Queries;

namespace VariableCompensation.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMIN")]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator mediator;

    public UsersController(IMediator mediator) => this.mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(new GetAdminUsersListQuery(), cancellationToken);
        return this.Ok(result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateAdminUserRequest request, CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new UpdateAdminUserCommand(id, request.Email, request.IsActive, request.RoleCodes, request.ControllerEmployeeId),
            cancellationToken);

        return result.IsSuccess ? this.Ok(result.Value) : this.BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:long}/password")]
    public async Task<IActionResult> ResetPassword(
        long id,
        [FromBody] ResetAdminUserPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await this.mediator.Send(
            new ResetAdminUserPasswordCommand(id, request.NewPassword),
            cancellationToken);

        return result.IsSuccess ? this.Ok() : this.BadRequest(new { error = result.Error });
    }
}
