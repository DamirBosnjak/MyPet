using MyPet.Services;
using System.Globalization;
using SQLite;

namespace MyPet.Models;

public class Pet
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Species { get; set; } = string.Empty;

    public string Breed { get; set; } = string.Empty;

    public string? Gender { get; set; }

    public DateTime? BirthDate { get; set; }

    public string? Notes { get; set; }

    [Ignore]
    public string Description =>
        string.IsNullOrWhiteSpace(Breed)
            ? Species
            : $"{Species} · {Breed}";

    [Ignore]
    public string GenderText =>
        string.IsNullOrWhiteSpace(Gender) ? "Not specified" : Gender;

    [Ignore]
    public string BirthDateText =>
        BirthDate.HasValue
            ? BirthDate.Value.ToString(
                "dd MMM yyyy",
                CultureInfo.InvariantCulture)
            : "Not specified";

    [Ignore]
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    
    public string? PhotoFileName { get; set; }

    [Ignore]
    public string? PhotoPath =>
        PetPhotoService.GetPhotoPath(PhotoFileName);

    [Ignore]
    public bool HasPhoto =>
        PhotoPath is string path && File.Exists(path);

    [Ignore]
    public bool HasNoPhoto => !HasPhoto;
    
}