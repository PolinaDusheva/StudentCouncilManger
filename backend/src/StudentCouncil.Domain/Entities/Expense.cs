using StudentCouncil.Domain.Common;

namespace StudentCouncil.Domain.Entities;

public class Expense : BaseEntity
{
    public string Description { get; set; } = string.Empty;

    /// <summary>Amount in euro, stored as decimal(10,2).</summary>
    public decimal AmountEur { get; set; }

    public DateOnly SpentOn { get; set; }

    /// <summary>Derived from <see cref="SpentOn"/> for cheap year filtering.</summary>
    public int Year { get; set; }

    /// <summary>FK -> Member (ApplicationUser).</summary>
    public Guid AddedById { get; set; }
}
