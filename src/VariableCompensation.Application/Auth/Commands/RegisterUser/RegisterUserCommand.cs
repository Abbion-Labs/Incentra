using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Auth.Models;

namespace VariableCompensation.Application.Auth.Commands.RegisterUser;

public sealed record RegisterUserCommand(string Email, string Password, IReadOnlyList<string> RoleCodes)
    : IRequest<Result<UserProfileResponse>>;
