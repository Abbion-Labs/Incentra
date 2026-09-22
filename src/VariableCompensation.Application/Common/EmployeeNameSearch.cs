using VariableCompensation.Domain.Entities.Hr;

namespace VariableCompensation.Application.Common;

/// <summary>
/// One shared rule for searching people by name, so every list behaves the same.
/// The term is matched against the full name in both orders, which means "Luka",
/// "Petrović", "Luka P" and "Petrović Luka" all find Luka Petrović. Matching the
/// parts separately would miss anything typed across the space.
/// </summary>
public static class EmployeeNameSearch
{
    /// <summary>
    /// Lower-cases the term and collapses runs of whitespace, or returns null when
    /// there is nothing to search for.
    /// </summary>
    public static string? Normalize(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var parts = search.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts).ToLowerInvariant();
    }

    public static IQueryable<Employee> Apply(IQueryable<Employee> query, string? search)
    {
        var term = Normalize(search);

        return term is null
            ? query
            : query.Where(e =>
                (e.FirstName + " " + e.LastName).ToLower().Contains(term) ||
                (e.LastName + " " + e.FirstName).ToLower().Contains(term));
    }
}
