using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Common;
using VariableCompensation.Application.Hr.Models;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Lookup;

namespace VariableCompensation.Application.Hr.OrganizationUnits.Commands;

public sealed record CreateOrganizationUnitCommand(string Name, string? Code) : IRequest<Result<OrganizationUnitResponse>>;

public sealed class CreateOrganizationUnitCommandHandler : IRequestHandler<CreateOrganizationUnitCommand, Result<OrganizationUnitResponse>>
{
    private readonly IOrganizationUnitRepository repository;

    public CreateOrganizationUnitCommandHandler(IOrganizationUnitRepository repository) => this.repository = repository;

    public async Task<Result<OrganizationUnitResponse>> Handle(CreateOrganizationUnitCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<OrganizationUnitResponse>(ErrorCodes.NameRequired);
        }

        if (await this.repository.NameExistsAsync(name, null, cancellationToken))
        {
            return Result.Failure<OrganizationUnitResponse>(ErrorCodes.ValueAlreadyExists);
        }

        var entity = new OrganizationUnit
        {
            Name = name,
            Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
            IsActive = true
        };

        await this.repository.AddAsync(entity, cancellationToken);
        await this.repository.SaveChangesAsync(cancellationToken);
        return Result.Success(HrMappings.ToResponse(entity));
    }
}

public sealed record UpdateOrganizationUnitCommand(long Id, string Name, string? Code, bool IsActive, int? Version) : IRequest<Result<OrganizationUnitResponse>>;

public sealed class UpdateOrganizationUnitCommandHandler : IRequestHandler<UpdateOrganizationUnitCommand, Result<OrganizationUnitResponse>>
{
    private readonly IOrganizationUnitRepository repository;

    public UpdateOrganizationUnitCommandHandler(IOrganizationUnitRepository repository) => this.repository = repository;

    public async Task<Result<OrganizationUnitResponse>> Handle(UpdateOrganizationUnitCommand request, CancellationToken cancellationToken)
    {
        var entity = await this.repository.FindByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<OrganizationUnitResponse>(ErrorCodes.OrganizationUnitNotFound);
        }

        var version = EditVersion.Claim(entity, request.Version);
        if (version.IsFailure)
        {
            return Result.Failure<OrganizationUnitResponse>(version.Error);
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<OrganizationUnitResponse>(ErrorCodes.NameRequired);
        }

        if (await this.repository.NameExistsAsync(name, request.Id, cancellationToken))
        {
            return Result.Failure<OrganizationUnitResponse>(ErrorCodes.ValueAlreadyExists);
        }

        entity.Name = name;
        entity.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await this.repository.SaveChangesAsync(cancellationToken);
        return Result.Success(HrMappings.ToResponse(entity));
    }
}
