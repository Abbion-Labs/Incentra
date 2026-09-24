using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Auth.Models;

namespace VariableCompensation.Application.Auth.Commands.RegisterUser;

/// <summary>
/// Creates an account. <paramref name="EmployeeId"/> is the employee the account belongs to: required for the
/// roles that act as an employee, and linked in the same step. <paramref name="ControllerEmployeeId"/> is the
/// controller of a new evaluator, or none for an evaluator whose evaluations need no review.
/// </summary>
public sealed record RegisterUserCommand(
    string Email,
    string Password,
    IReadOnlyList<string> RoleCodes,
    long? EmployeeId = null,
    long? ControllerEmployeeId = null)
    : IRequest<Result<UserProfileResponse>>;
