using MyPet.Models;
using SQLite;

namespace MyPet.Services;

public class PetDatabase
{
    private SQLiteAsyncConnection? _database;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    private async Task Init()
    {
        if (_initialized)
            return;

        await _initLock.WaitAsync();

        try
        {
            if (_initialized)
                return;

            string databasePath = Path.Combine(
                FileSystem.AppDataDirectory,
                "mypet.db3");

            _database ??= new SQLiteAsyncConnection(databasePath);

            await _database.CreateTableAsync<Pet>();
            await _database.CreateTableAsync<HealthEvent>();

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task<List<Pet>> GetPetsAsync()
    {
        await Init();

        return await _database!
            .Table<Pet>()
            .ToListAsync();
    }

    public async Task<Pet?> GetPetAsync(int id)
    {
        await Init();

        return await _database!
            .Table<Pet>()
            .Where(pet => pet.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> SavePetAsync(Pet pet)
    {
        await Init();

        if (pet.Id != 0)
            return await _database!.UpdateAsync(pet);

        return await _database!.InsertAsync(pet);
    }

    public async Task<int> DeletePetAsync(Pet pet)
    {
        await Init();

        int deletedRows = 0;

        await _database!.RunInTransactionAsync(connection =>
        {
            connection.Execute(
                "DELETE FROM HealthEvent WHERE PetId = ?",
                pet.Id);

            deletedRows = connection.Delete<Pet>(pet.Id);
        });

        if (deletedRows > 0)
        {
            PetPhotoService.DeleteSavedPhoto(pet.PhotoFileName);
        }
        
        return deletedRows;
    }

    public async Task<List<HealthEvent>> GetHealthEventsAsync(int petId)
    {
        await Init();

        return await _database!
            .Table<HealthEvent>()
            .Where(healthEvent => healthEvent.PetId == petId)
            .OrderByDescending(healthEvent => healthEvent.Date)
            .ToListAsync();
    }

    public async Task<HealthEvent?> GetHealthEventAsync(int id)
    {
        await Init();

        return await _database!
            .Table<HealthEvent>()
            .Where(healthEvent => healthEvent.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> SaveHealthEventAsync(HealthEvent healthEvent)
    {
        if (string.IsNullOrWhiteSpace(healthEvent.Title))
            throw new ArgumentException("An event title is required.");

        if (!Enum.IsDefined(typeof(HealthEventType), healthEvent.Type))
            throw new ArgumentException("Invalid event type.");

        await Init();

        healthEvent.Title = healthEvent.Title.Trim();
        healthEvent.Notes = healthEvent.Notes?.Trim() ?? string.Empty;
        healthEvent.Date = healthEvent.Date.Date;

        int affectedRows = 0;

        await _database!.RunInTransactionAsync(connection =>
        {
            var pet = connection.Find<Pet>(healthEvent.PetId);

            if (pet is null)
                throw new InvalidOperationException("The pet no longer exists.");

            if (healthEvent.Id == 0)
            {
                affectedRows = connection.Insert(healthEvent);
            }
            else
            {
                var existing = connection.Find<HealthEvent>(healthEvent.Id);

                if (existing is null || existing.PetId != healthEvent.PetId)
                {
                    throw new InvalidOperationException(
                        "The event is no longer available for this pet.");
                }

                affectedRows = connection.Update(healthEvent);
            }
        });

        return affectedRows;
    }

    public async Task<int> DeleteHealthEventAsync(HealthEvent healthEvent)
    {
        await Init();

        return await _database!.DeleteAsync(healthEvent);
    }
    
    public async Task<List<HealthEvent>> GetAllHealthEventsAsync()
    {
        await Init();

        return await _database!
            .Table<HealthEvent>()
            .ToListAsync();
    }
}