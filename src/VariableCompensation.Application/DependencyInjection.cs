using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using VariableCompensation.Application.Abstractions.Notifications;
using VariableCompensation.Application.Auth;
using VariableCompensation.Application.Compensation.Services;
using VariableCompensation.Application.Evaluation.Services;
using VariableCompensation.Application.Hr.Services;
using VariableCompensation.Application.Notifications;
namespace VariableCompensation.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<AuthSessionIssuer>();
        services.AddScoped<EvaluationScoringService>();
        services.AddScoped<EvaluationAccessService>();
        services.AddScoped<ControllerSupervisionService>();
        services.AddScoped<EmployeeAccessService>();
        services.AddScoped<CompensationCalculationService>();
        services.AddScoped<CompensationAccessService>();
        services.AddScoped<CompensationAnalyticsService>();
        services.AddScoped<IEvaluationNotificationService, EvaluationNotificationService>();
        return services;
    }
}
