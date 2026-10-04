using System.Globalization;
using SQLite;

namespace MyPet.Models;

public enum HealthEventType
{
    Vaccination,
    VetVisit,
    Treatment
}

public class HealthEvent
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int PetId { get; set; }

    public string Title { get; set; } = string.Empty;

    public HealthEventType Type { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    public string Notes { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }

    [Ignore]
    public string TypeText => Type switch
    {
        HealthEventType.Vaccination => "Vaccination",
        HealthEventType.VetVisit => "Vet visit",
        HealthEventType.Treatment => "Treatment",
        _ => "Other"
    };

    [Ignore]
    public string DateText =>
        Date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    [Ignore]
    public bool IsOverdue =>
        !IsCompleted && Date.Date < DateTime.Today;

    [Ignore]
    public string StatusText =>
        IsCompleted ? "Completed" :
        IsOverdue ? "Overdue" :
        "Planned";
}