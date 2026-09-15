namespace VariableCompensation.Application.Common.Models;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages => this.PageSize == 0 ? 0 : (int)Math.Ceiling(this.TotalCount / (double)this.PageSize);
}
