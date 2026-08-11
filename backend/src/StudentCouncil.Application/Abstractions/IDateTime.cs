namespace StudentCouncil.Application.Abstractions;

/// <summary>Abstracts the system clock so time-dependent logic stays testable.</summary>
public interface IDateTime
{
    DateTime UtcNow { get; }
}
