using CSharpFunctionalExtensions;
using MediatR;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Application.Hr.Models;
using VariableCompensation.Domain;
using VariableCompensation.Domain.Entities.Lookup;

namespace VariableCompensation.Application.Hr.JobPositions.Commands;

public sealed record CreateJobPositionCommand(string Name, int SortOrder) : IRequest<Result<JobPositionResponse>>;

public sealed class CreateJobPositionCommandHandler : IRequestHandler<CreateJobPositionCommand, Result<JobPositionResponse>>
{
    private readonly IJobPositionRepository repository;

    public CreateJobPositionCommandHandler(IJobPositionRepository repository) => this.repository = repository;

    public async Task<Result<JobPositionResponse>> Handle(CreateJobPositionCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<JobPositionResponse>(ErrorCodes.NameRequired);
        }

        if (await this.repository.NameExistsAsync(name, null, cancellationToken))
        {
            return Result.Failure<JobPositionResponse>(ErrorCodes.ValueAlreadyExists);
        }

        var entity = new JobPosition { Name = name, SortOrder = request.SortOrder, IsActive = true };
        await this.repository.AddAsync(entity, cancellationToken);
        await this.repository.SaveChangesAsync(cancellationToken);
        return Result.Success(HrMappings.ToResponse(entity));
    }
}

public sealed record UpdateJobPositionCommand(long Id, string Name, int SortOrder, bool IsActive) : IRequest<Result<JobPositionResponse>>;

public sealed class UpdateJobPositionCommandHandler : IRequestHandler<UpdateJobPositionCommand, Result<JobPositionResponse>>
{
    private readonly IJobPositionRepository repository;

    public UpdateJobPositionCommandHandler(IJobPositionRepository repository) => this.repository = repository;

    public async Task<Result<JobPositionResponse>> Handle(UpdateJobPositionCommand request, CancellationToken cancellationToken)
    {
        var entity = await this.repository.FindByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<JobPositionResponse>(ErrorCodes.JobPositionNotFound);
        }

        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<JobPositionResponse>(ErrorCodes.NameRequired);
        }

        if (await this.repository.NameExistsAsync(name, request.Id, cancellationToken))
        {
            return Result.Failure<JobPositionResponse>(ErrorCodes.ValueAlreadyExists);
        }

        entity.Name = name;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await this.repository.SaveChangesAsync(cancellationToken);
        return Result.Success(HrMappings.ToResponse(entity));
    }
}
