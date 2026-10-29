namespace VariableCompensation.Domain.Common;

/// <summary>
/// A record edited through a form. Its version grows with every edit, and an edit states the version it was made
/// from, so an edit made from an outdated copy is refused instead of silently undoing a newer one.
/// </summary>
public interface IVersioned
{
    int Version { get; set; }
}
