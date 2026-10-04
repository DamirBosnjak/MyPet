using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace MyPet.Services;

public static class PetPhotoService
{
    public static string? GetPhotoPath(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        return Path.Combine(
            FileSystem.AppDataDirectory,
            "pet_photos",
            Path.GetFileName(fileName));
    }

    public static async Task<string?> PickToCacheAsync()
    {
        var photos = await MediaPicker.Default.PickPhotosAsync(
            new MediaPickerOptions
            {
                SelectionLimit = 1,
                MaximumWidth = 1200,
                MaximumHeight = 1200,
                CompressionQuality = 85,
                RotateImage = true,
                PreserveMetaData = false
            });

        var photo = photos.FirstOrDefault();

        if (photo is null)
            return null;

        using var source = await photo.OpenReadAsync();

        return await CopyPhotoAsync(
            source,
            Path.Combine(FileSystem.CacheDirectory, "pet_photo_drafts"),
            Path.GetExtension(photo.FileName));
    }

    //Prebacuje izabranu fotografiju iz privremenog cache-a u trajni folder aplikacije
    public static async Task<string> SaveFromCacheAsync(string cachedPath)
    {
        using var source = File.OpenRead(cachedPath);

        string savedPath = await CopyPhotoAsync(
            source,
            Path.Combine(FileSystem.AppDataDirectory, "pet_photos"),
            Path.GetExtension(cachedPath));

        return Path.GetFileName(savedPath);
    }

    private static async Task<string> CopyPhotoAsync(
        Stream source,
        string directory,
        string extension)
    {
        Directory.CreateDirectory(directory);

        if (string.IsNullOrWhiteSpace(extension))
            extension = ".jpg";

        string destinationPath = Path.Combine(
            directory,
            $"{Guid.NewGuid():N}{extension}");

        try
        {
            using var destination = File.Create(destinationPath);
            await source.CopyToAsync(destination);
        }
        catch
        {
            DeleteTemporaryPhoto(destinationPath);
            throw;
        }

        return destinationPath;
    }

    public static void DeleteSavedPhoto(string? fileName)
    {
        DeleteTemporaryPhoto(GetPhotoPath(fileName));
    }

    public static void DeleteTemporaryPhoto(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}