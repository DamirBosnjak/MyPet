namespace MyPet.Models;

public class CareItem
{
    public HealthEvent Event { get; set; } = new();
    
    public string PetName {get; set;} = string.Empty;
}