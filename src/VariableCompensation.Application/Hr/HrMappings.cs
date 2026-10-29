using VariableCompensation.Application.Hr.Models;
using VariableCompensation.Domain.Entities.Hr;
using VariableCompensation.Domain.Entities.Lookup;
using EvaluatorSettingsEntity = VariableCompensation.Domain.Entities.Hr.EvaluatorSettings;

namespace VariableCompensation.Application.Hr;

internal static class HrMappings
{
    public static OrganizationUnitResponse ToResponse(OrganizationUnit entity) =>
        new()
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            Code = entity.Code,
            IsActive = entity.IsActive
        };

    public static JobPositionResponse ToResponse(JobPosition entity) =>
        new()
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            SortOrder = entity.SortOrder,
            IsActive = entity.IsActive
        };

    public static EducationLevelResponse ToResponse(EducationLevel entity) =>
        new()
        {
            Id = entity.Id,
            Version = entity.Version,
            Name = entity.Name,
            SortOrder = entity.SortOrder,
            IsActive = entity.IsActive
        };

    public static EmployeeResponse ToResponse(Employee entity) =>
        new()
        {
            Id = entity.Id,
            Version = entity.Version,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            FullName = entity.FullName,
            OrganizationUnitId = entity.OrganizationUnitId,
            OrganizationUnitName = entity.OrganizationUnit?.Name ?? string.Empty,
            JobPositionId = entity.JobPositionId,
            JobPositionName = entity.JobPosition?.Name ?? string.Empty,
            EducationLevelId = entity.EducationLevelId,
            EducationLevelName = entity.EducationLevel?.Name,
            EvaluatorEmployeeId = entity.EvaluatorEmployeeId,
            EvaluatorFullName = entity.Evaluator?.FullName,
            UserId = entity.UserId,
            IsActive = entity.IsActive,
            HiredAt = entity.HiredAt,
            AvatarUrl = entity.AvatarUrl
        };

    public static ControllerEvaluatorSummaryResponse ToControllerEvaluatorSummary(
        EvaluatorSettingsEntity entity,
        int subordinateCount) =>
        new()
        {
            EmployeeId = entity.EmployeeId,
            EmployeeFullName = entity.Employee.FullName,
            OrganizationUnitName = entity.Employee.OrganizationUnit?.Name ?? string.Empty,
            JobPositionName = entity.Employee.JobPosition?.Name ?? string.Empty,
            AvatarUrl = entity.Employee.AvatarUrl,
            SubordinateCount = subordinateCount,
        };

    public static EvaluatorSettingsResponse ToResponse(EvaluatorSettingsEntity entity) =>
        new()
        {
            EmployeeId = entity.EmployeeId,
            Version = entity.Version,
            EmployeeFullName = entity.Employee.FullName,
            ControllerEmployeeId = entity.ControllerEmployeeId,
            ControllerFullName = entity.Controller?.FullName
        };
}
