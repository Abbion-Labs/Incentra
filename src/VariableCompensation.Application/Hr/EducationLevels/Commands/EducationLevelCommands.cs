using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Hr.Models;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Lookup;

namespace VariableCompensation.Application.Hr.EducationLevels.Commands;

public sealed record CreateEducationLevelCommand(string Name, int SortOrder) : IRequest<Result<EducationLevelResponse>>;

public sealed class CreateEducationLevelCommandHandler : IRequestHandler<CreateEducationLevelCommand, Result<EducationLevelResponse>>
{
    private readonly IEducationLevelRepository repository;

    public CreateEducationLevelCommandHandler(IEducationLevelRepository repository) => this.repository = repository;

    public async Task<Result<EducationLevelResponse>> Handle(CreateEducationLevelCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<EducationLevelResponse>(ErrorCodes.NameRequired);
        }

        if (await this.repository.NameExistsAsync(name, null, cancellationToken))
        {
            return Result.Failure<EducationLevelResponse>(ErrorCodes.ValueAlreadyExists);
        }

        var entity = new EducationLevel { Name = name, SortOrder = request.SortOrder, IsActive = true };
        await this.repository.AddAsync(entity, cancellationToken);
        await this.repository.SaveChangesAsync(cancellationToken);
        return Result.Success(HrMappings.ToResponse(entity));
    }
}

public sealed record UpdateEducationLevelCommand(long Id, string Name, int SortOrder, bool IsActive) : IRequest<Result<EducationLevelResponse>>;

public sealed class UpdateEducationLevelCommandHandler : IRequestHandler<UpdateEducationLevelCommand, Result<EducationLevelResponse>>
{
    private readonly IEducationLevelRepository repository;

    public UpdateEducationLevelCommandHandler(IEducationLevelRepository repository) => this.repository = repository;

    public async Task<Result<EducationLevelResponse>> Handle(UpdateEducationLevelCommand request, CancellationToken cancellationToken)
    {
        var entity = await this.repository.FindByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<EducationLevelResponse>(ErrorCodes.EducationLevelNotFound);
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<EducationLevelResponse>(ErrorCodes.NameRequired);
        }

        if (await this.repository.NameExistsAsync(name, request.Id, cancellationToken))
        {
            return Result.Failure<EducationLevelResponse>(ErrorCodes.ValueAlreadyExists);
        }

        entity.Name = name;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await this.repository.SaveChangesAsync(cancellationToken);
        return Result.Success(HrMappings.ToResponse(entity));
    }
}
