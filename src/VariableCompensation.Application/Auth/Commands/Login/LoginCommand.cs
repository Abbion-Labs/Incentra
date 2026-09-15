using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Auth.Models;

namespace VariableCompensation.Application.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;
