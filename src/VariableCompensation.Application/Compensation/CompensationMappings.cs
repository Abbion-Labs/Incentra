using VariableCompensation.Application.Compensation.Models;
using VariableCompensation.Domain.Entities.Compensation;
using VariableCompensation.Domain.Entities.Evaluation;

namespace VariableCompensation.Application.Compensation;

internal static class CompensationMappings
{
    public static CompensationParametersResponse ToResponse(VariableCompensationParameters entity) =>
        new()
        {
            Id = entity.Id,
            OrganizationUnitId = entity.OrganizationUnitId,
            OrganizationUnitName = entity.OrganizationUnit?.Name ?? string.Empty,
            Year = entity.Year,
            MonetaryPool = entity.MonetaryPool,
            Currency = entity.Currency,
            AcceptablePerformanceRating = entity.AcceptablePerformanceRating,
            UpperLimitCoefficient = entity.UpperLimitCoefficient,
            DependencyWeight = entity.DependencyWeight,
            Exponent = entity.Exponent,
            AllowNegativeVariable = entity.AllowNegativeVariable,
            IsActive = entity.IsActive
        };

    public static CompensationResultSummaryResponse ToSummary(VariableCompensationResult entity) =>
        new()
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeFullName = entity.Employee.FullName,
            OrganizationUnitName = entity.Employee.OrganizationUnit?.Name ?? entity.Parameters.OrganizationUnit?.Name ?? string.Empty,
            ParametersId = entity.ParametersId,
            Year = entity.Year,
            OverallAverage = entity.OverallAverage,
            Points = entity.Points,
            SalaryPointsValue = entity.SalaryPointsValue,
            FixedSalary = entity.Points * entity.SalaryPointsValue,
            CompensationPercent = entity.CompensationPercent,
            NetCompensation = entity.NetCompensation,
            QuarterlyCompensation = entity.QuarterlyCompensation,
            MonthlyCompensation = entity.MonthlyCompensation,
            IsFinal = entity.IsFinal,
            CalculatedAt = entity.CalculatedAt
        };

    public static CompensationResultDetailResponse ToDetail(VariableCompensationResult entity)
    {
        var summary = ToSummary(entity);
        return new CompensationResultDetailResponse
        {
            Id = summary.Id,
            EmployeeId = summary.EmployeeId,
            EmployeeFullName = summary.EmployeeFullName,
            OrganizationUnitName = summary.OrganizationUnitName,
            ParametersId = summary.ParametersId,
            Year = summary.Year,
            OverallAverage = summary.OverallAverage,
            Points = summary.Points,
            SalaryPointsValue = summary.SalaryPointsValue,
            FixedSalary = summary.FixedSalary,
            CompensationPercent = summary.CompensationPercent,
            NetCompensation = summary.NetCompensation,
            QuarterlyCompensation = summary.QuarterlyCompensation,
            MonthlyCompensation = summary.MonthlyCompensation,
            IsFinal = summary.IsFinal,
            CalculatedAt = summary.CalculatedAt,
            GoalsAverage = entity.GoalsAverage,
            MeasuresAverage = entity.MeasuresAverage,
            Weight = entity.Weight,
            ZScore = entity.ZScore,
            NormalizedZScore = entity.NormalizedZScore,
            NormalizedPoints = entity.NormalizedPoints,
            CompensationWithoutSalary = entity.CompensationWithoutSalary,
            Aq = entity.Aq,
            FormulaVersion = entity.FormulaVersion,
            EvaluationLinks = entity.EvaluationLinks
                .OrderBy(l => l.Evaluation.Quarter)
                .Select(ToLink)
                .ToList()
        };
    }

    public static CompensationEvaluationLinkResponse ToLink(VariableCompensationEvaluationLink link) =>
        new()
        {
            EvaluationId = link.EvaluationId,
            Quarter = link.Evaluation.Quarter,
            OverallAverage = link.Evaluation.OverallAverage ?? 0,
            ApprovedAt = link.Evaluation.ApprovedAt
        };
}
