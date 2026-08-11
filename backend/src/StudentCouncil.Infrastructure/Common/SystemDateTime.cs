using StudentCouncil.Application.Abstractions;

namespace StudentCouncil.Infrastructure.Common;

public sealed class SystemDateTime : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
}
