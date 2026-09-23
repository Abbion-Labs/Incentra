using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Abstractions.Security;
using VariableCompensation.Application.Common;
using VariableCompensation.Application.Hr.Models;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Enums;

namespace VariableCompensation.Application.Hr.EmployeeSalaries.Commands;

public sealed record UpsertEmployeeSalaryCommand(
    long EmployeeId,
    int Points,
    decimal SalaryPerPoint,
    DateOnly EffectiveFrom,
    string Currency,
    int? Version) : IRequest<Result<EmployeeSalaryResponse>>;

public sealed class UpsertEmployeeSalaryCommandHandler : IRequestHandler<UpsertEmployeeSalaryCommand, Result<EmployeeSalaryResponse>>
{
    private readonly IEmployeeRepository employeeRepository;
    private readonly IEmployeeSalaryRepository salaryRepository;
    private readonly ISensitiveDataEncryptionService encryptionService;
    private readonly IAuditLogWriter auditLogWriter;
    private readonly ICurrentUserService currentUserService;

    public UpsertEmployeeSalaryCommandHandler(
        IEmployeeRepository employeeRepository,
        IEmployeeSalaryRepository salaryRepository,
        ISensitiveDataEncryptionService encryptionService,
        IAuditLogWriter auditLogWriter,
        ICurrentUserService currentUserService)
    {
        this.employeeRepository = employeeRepository;
        this.salaryRepository = salaryRepository;
        this.encryptionService = encryptionService;
        this.auditLogWriter = auditLogWriter;
        this.currentUserService = currentUserService;
    }

    public async Task<Result<EmployeeSalaryResponse>> Handle(UpsertEmployeeSalaryCommand request, CancellationToken cancellationToken)
    {
        if (!this.currentUserService.IsInRole(RoleCodes.Payroll))
        {
            return Result.Failure<EmployeeSalaryResponse>(ErrorCodes.Forbidden);
        }

        if (request.Points <= 0 || request.Points > 1000)
        {
            return Result.Failure<EmployeeSalaryResponse>(ErrorCodes.PointsRangeInvalid);
        }

        if (request.SalaryPerPoint <= 0)
        {
            return Result.Failure<EmployeeSalaryResponse>(ErrorCodes.SalaryPerPointInvalid);
        }

        var employee = await this.employeeRepository.FindByIdAsync(request.EmployeeId, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<EmployeeSalaryResponse>(ErrorCodes.EmployeeNotFound);
        }

        var encryptedSalaryPerPoint = this.encryptionService.EncryptDecimal(request.SalaryPerPoint);
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "RSD" : request.Currency.Trim().ToUpperInvariant();
        var current = await this.salaryRepository.FindCurrentByEmployeeIdForUpdateAsync(request.EmployeeId, cancellationToken);

        // The version is the one of the salary in force the edit was made from, and absent only for an employee who
        // had no salary yet. Two first salaries entered at once are stopped by the one-current-salary index.
        if (current is not null)
        {
            var version = EditVersion.Claim(current, request.Version);
            if (version.IsFailure)
            {
                return Result.Failure<EmployeeSalaryResponse>(version.Error);
            }
        }
        else if (request.Version is not null)
        {
            return Result.Failure<EmployeeSalaryResponse>(ErrorCodes.ConcurrencyConflict);
        }

        EmployeeSalary saved;

        if (current is null)
        {
            saved = new EmployeeSalary
            {
                EmployeeId = request.EmployeeId,
                Points = request.Points,
                EncryptedSalaryPerPoint = encryptedSalaryPerPoint,
                Currency = currency,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveTo = null,
                CreatedByUserId = this.currentUserService.UserId,
                UpdatedByUserId = this.currentUserService.UserId,
            };
            await this.salaryRepository.AddAsync(saved, cancellationToken);
            await this.auditLogWriter.WriteAsync(
                "EmployeeSalary",
                request.EmployeeId,
                "Create",
                null,
                "{\"salaryChanged\":true}",
                cancellationToken);
        }
        else
        {
            var currentSalaryPerPoint = this.encryptionService.DecryptDecimal(current.EncryptedSalaryPerPoint);
            if (current.Points == request.Points
                && currentSalaryPerPoint == request.SalaryPerPoint
                && current.EffectiveFrom == request.EffectiveFrom
                && current.Currency == currency)
            {
                saved = current;
            }
            else
            {
                if (request.EffectiveFrom <= current.EffectiveFrom)
                {
                    return Result.Failure<EmployeeSalaryResponse>(ErrorCodes.EffectiveDateMustBeAfterCurrent);
                }

                current.EffectiveTo = request.EffectiveFrom.AddDays(-1);
                current.UpdatedAt = DateTime.UtcNow;
                current.UpdatedByUserId = this.currentUserService.UserId;

                saved = new EmployeeSalary
                {
                    EmployeeId = request.EmployeeId,
                    Points = request.Points,
                    EncryptedSalaryPerPoint = encryptedSalaryPerPoint,
                    Currency = currency,
                    EffectiveFrom = request.EffectiveFrom,
                    EffectiveTo = null,
                    CreatedByUserId = this.currentUserService.UserId,
                    UpdatedByUserId = this.currentUserService.UserId,
                };
                await this.salaryRepository.AddAsync(saved, cancellationToken);
                await this.auditLogWriter.WriteAsync(
                    "EmployeeSalary",
                    request.EmployeeId,
                    "VersionCreate",
                    null,
                    "{\"salaryChanged\":true}",
                    cancellationToken);
            }
        }

        await this.salaryRepository.SaveChangesAsync(cancellationToken);

        return Result.Success(new EmployeeSalaryResponse
        {
            Id = saved.Id,
            Version = saved.Version,
            EmployeeId = request.EmployeeId,
            EmployeeFullName = employee.FullName,
            OrganizationUnitName = employee.OrganizationUnit?.Name ?? string.Empty,
            Points = saved.Points,
            SalaryPerPoint = request.SalaryPerPoint,
            Currency = saved.Currency,
            EffectiveFrom = saved.EffectiveFrom,
            EffectiveTo = saved.EffectiveTo,
            IsCurrent = saved.EffectiveTo is null,
            UpdatedAt = saved.UpdatedAt,
        });
    }
}
