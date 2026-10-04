using MyPet.Models;
using MyPet.Services;

namespace MyPet.Views;

public partial class PetFormPage : ContentPage
{
    private readonly PetDatabase _database;

    private int _petId;
    private bool _isSaving;
    private bool _saved;
    private bool _isPickingPhoto;

    private string? _photoFileName;
    private string? _pendingPhotoPath;
    private bool _removePhoto;

    public PetFormPage(PetDatabase database, Pet? pet = null)
    {
        InitializeComponent();
        _database = database;

        GenderPicker.SelectedIndex = -1;

        BirthDatePicker.MaximumDate = DateTime.Today;
        BirthDatePicker.Date = DateTime.Today;

        if (pet is not null)
        {
            _petId = pet.Id;
            _photoFileName = pet.PhotoFileName;

            Title = "Edit Pet";
            FormHeading.Text = "Update your pet's profile";
            SaveButton.Text = "Save changes";

            NameEntry.Text = pet.Name;
            SpeciesEntry.Text = pet.Species;
            BreedEntry.Text = pet.Breed;
            NotesEditor.Text = pet.Notes;

            GenderPicker.SelectedIndex = pet.Gender switch
            {
                "Male" => 0,
                "Female" => 1,
                _ => -1
            };

            if (pet.BirthDate.HasValue)
            {
                BirthDatePicker.Date = pet.BirthDate.Value;
            }

            BirthDateKnownSwitch.IsToggled = pet.BirthDate.HasValue;
        }

        UpdatePhotoPreview();
    }

    private void OnBirthDateKnownToggled(
        object? sender,
        ToggledEventArgs e)
    {
        BirthDatePicker.IsVisible = e.Value;
    }

    private async void OnChoosePhotoClicked(object? sender, EventArgs e)
    {
        if (_isPickingPhoto || _isSaving || _saved)
            return;

        _isPickingPhoto = true;
        ErrorLabel.IsVisible = false;
        UpdatePhotoPreview();

        try
        {
            string? selectedPath =
                await PetPhotoService.PickToCacheAsync();

            if (selectedPath is null)
                return;

            PetPhotoService.DeleteTemporaryPhoto(_pendingPhotoPath);

            _pendingPhotoPath = selectedPath;
            _removePhoto = false;
        }
        catch (OperationCanceledException)
        {
            // Closing the picker leaves the current photo unchanged.
        }
        catch (PermissionException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            ShowError(
                "Photo access was denied. Check MyPet's permissions in Settings.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ShowError("Could not load this photo. Please try another image.");
        }
        finally
        {
            _isPickingPhoto = false;
            UpdatePhotoPreview();
        }
    }

    private void OnRemovePhotoClicked(object? sender, EventArgs e)
    {
        if (_isPickingPhoto || _isSaving || _saved)
            return;

        PetPhotoService.DeleteTemporaryPhoto(_pendingPhotoPath);

        _pendingPhotoPath = null;
        _removePhoto = true;

        UpdatePhotoPreview();
    }

    private void UpdatePhotoPreview()
    {
        string? path = _removePhoto
            ? null
            : _pendingPhotoPath
              ?? PetPhotoService.GetPhotoPath(_photoFileName);

        bool hasPhoto = path is not null && File.Exists(path);

        PhotoPreview.Source = hasPhoto
            ? ImageSource.FromFile(path!)
            : null;

        PhotoPreview.IsVisible = hasPhoto;
        PhotoPlaceholder.IsVisible = !hasPhoto;

        bool enabled = !_isPickingPhoto && !_isSaving && !_saved;

        ChoosePhotoButton.IsEnabled = enabled;
        RemovePhotoButton.IsEnabled = enabled && hasPhoto;
        SaveButton.IsEnabled = enabled;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_isSaving || _saved || _isPickingPhoto)
            return;

        string name = NameEntry.Text?.Trim() ?? string.Empty;
        string species = SpeciesEntry.Text?.Trim() ?? string.Empty;
        string breed = BreedEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Enter the pet's name.");
            NameEntry.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(species))
        {
            ShowError("Enter the pet's species.");
            SpeciesEntry.Focus();
            return;
        }

        DateTime? birthDate = null;

        if (BirthDateKnownSwitch.IsToggled)
        {
            if (BirthDatePicker.Date is not DateTime selectedDate)
            {
                ShowError("Select a birth date.");
                return;
            }

            if (selectedDate.Date > DateTime.Today)
            {
                ShowError("Birth date cannot be in the future.");
                return;
            }

            birthDate = selectedDate.Date;
        }

        string? gender = GenderPicker.SelectedIndex switch
        {
            0 => "Male",
            1 => "Female",
            _ => null
        };

        var pet = new Pet
        {
            Id = _petId,
            Name = name,
            Species = species,
            Breed = breed,
            Gender = gender,
            BirthDate = birthDate,
            Notes = NotesEditor.Text?.Trim() ?? string.Empty,
            PhotoFileName = _removePhoto ? null : _photoFileName
        };

        string? previousPhotoFileName = _photoFileName;
        string? newPhotoFileName = null;

        _isSaving = true;
        ErrorLabel.IsVisible = false;
        UpdatePhotoPreview();

        try
        {
            if (_pendingPhotoPath is not null)
            {
                newPhotoFileName =
                    await PetPhotoService.SaveFromCacheAsync(_pendingPhotoPath);

                pet.PhotoFileName = newPhotoFileName;
            }

            int affectedRows = await _database.SavePetAsync(pet);

            if (affectedRows != 1)
            {
                ShowError(
                    "This pet could not be saved. Return to My Pets and try again.");
                return;
            }

            _petId = pet.Id;
            _saved = true;
            _photoFileName = pet.PhotoFileName;
            _removePhoto = false;

            // Remove the old photo only after the database save succeeds.
            if (previousPhotoFileName != pet.PhotoFileName)
            {
                PetPhotoService.DeleteSavedPhoto(previousPhotoFileName);
            }

            PetPhotoService.DeleteTemporaryPhoto(_pendingPhotoPath);
            _pendingPhotoPath = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ShowError("Could not save your pet. Please try again.");
            return;
        }
        finally
        {
            if (!_saved)
            {
                PetPhotoService.DeleteSavedPhoto(newPhotoFileName);
            }

            _isSaving = false;
            UpdatePhotoPreview();
        }

        try
        {
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ShowError("Your pet was saved. Use Back to return.");
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}