using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using VariableCompensation.Application.Abstractions.Auth;
using VariableCompensation.Application.Abstractions.Notifications;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Abstractions.Security;
using VariableCompensation.Application.Auth.Commands.RegisterUser;
using VariableCompensation.Infrastructure.Audit;
using VariableCompensation.Infrastructure.Auth;
using VariableCompensation.Infrastructure.Notifications;
using VariableCompensation.Infrastructure.Persistence.Repositories;
using VariableCompensation.Infrastructure.Security;

namespace VariableCompensation.Infrastructure;

public static class AuthDependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<LoginRateLimitOptions>(configuration.GetSection(LoginRateLimitOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ILoginAttemptLimiter, InMemoryLoginAttemptLimiter>();
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<Application.Notifications.EmailNotificationSettings>(
            configuration.GetSection(Application.Notifications.EmailNotificationSettings.SectionName));
        services.AddHttpContextAccessor();

        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentEmployeeContext, CurrentEmployeeContext>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleLookup, UserRepository>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IOrganizationUnitRepository, OrganizationUnitRepository>();
        services.AddScoped<IJobPositionRepository, JobPositionRepository>();
        services.AddScoped<IEducationLevelRepository, EducationLevelRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IEvaluationRepository, EvaluationRepository>();
        services.AddScoped<IEvaluationLookupRepository, EvaluationLookupRepository>();
        services.AddScoped<IDescriptiveRatingRepository, DescriptiveRatingRepository>();
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
        services.AddScoped<IEvaluatorSettingsRepository, EvaluatorSettingsRepository>();
        services.AddScoped<ICompensationRepository, CompensationRepository>();
        services.AddScoped<IEmployeeSalaryRepository, EmployeeSalaryRepository>();
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddSingleton<ISensitiveDataEncryptionService, AesSensitiveDataEncryptionService>();

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings are not configured.");

        if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32)
        {
            throw new InvalidOperationException("JWT secret must be at least 32 characters.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ActiveSessionValidator.ValidateAsync
                };
            });

        services.AddAuthorization();
        return services;
    }
}
