using Microsoft.EntityFrameworkCore;
using VariableCompensation.Application.Abstractions.Persistence;
using VariableCompensation.Domain.Entities.Lookup;

namespace VariableCompensation.Infrastructure.Persistence.Repositories;

public sealed class DescriptiveRatingRepository : IDescriptiveRatingRepository
{
    private readonly AppDbContext context;

    public DescriptiveRatingRepository(AppDbContext context) => this.context = context;

    public Task<DescriptiveRating?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
        this.context.DescriptiveRatings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken cancellationToken) =>
        this.context.DescriptiveRatings.AnyAsync(
            x => x.Code == code && (excludeId == null || x.Id != excludeId),
            cancellationToken);

    public async Task<IReadOnlyList<DescriptiveRating>> GetAllAsync(bool? isActive, CancellationToken cancellationToken)
    {
        var query = this.context.DescriptiveRatings.AsNoTracking().AsQueryable();
        if (isActive is not null)
        {
            query = query.Where(x => x.IsActive == isActive);
        }

        return await query.OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetRecommendedShareByCodeAsync(CancellationToken cancellationToken)
    {
        var rows = await this.context.DescriptiveRatings
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.Code, x => x.RecommendedShare);
    }

    public async Task AddAsync(DescriptiveRating entity, CancellationToken cancellationToken) =>
        await this.context.DescriptiveRatings.AddAsync(entity, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        this.context.SaveChangesAsync(cancellationToken);
}
