namespace StudentCouncil.Application.Common.Audit;

/// <summary>
/// Builds the <c>details</c> payload serialised to <c>audit_logs.details</c> (decision #4). For updates it
/// records only the fields that actually changed, each as a <c>{ from, to }</c> pair, so the audit is useful
/// for investigation without storing the whole entity. Sensitive values (passwords/tokens) are never passed
/// in (spec 14). Pass <see cref="Values"/> to <see cref="IAuditRecorder.Record(string,string,string,object)"/>.
/// </summary>
public sealed class AuditDetails
{
    private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

    /// <summary>The accumulated changes; serialise this (not the builder) into the audit row.</summary>
    public IReadOnlyDictionary<string, object?> Values => _values;

    /// <summary>True when at least one field actually changed.</summary>
    public bool HasChanges => _values.Count > 0;

    /// <summary>Records <paramref name="field"/> as a <c>{ from, to }</c> pair when the values differ.</summary>
    public AuditDetails Change<T>(string field, T before, T after)
    {
        if (!EqualityComparer<T>.Default.Equals(before, after))
        {
            _values[field] = new { from = before, to = after };
        }

        return this;
    }
}
