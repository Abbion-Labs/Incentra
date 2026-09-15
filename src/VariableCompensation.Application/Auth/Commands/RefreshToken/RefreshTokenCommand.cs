using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Auth.Models;

namespace VariableCompensation.Application.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResponse>>;
