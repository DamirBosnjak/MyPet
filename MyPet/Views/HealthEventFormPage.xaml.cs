using MyPet.Models;
using MyPet.Services;

namespace MyPet.Views;

public partial class HealthEventFormPage : ContentPage
{
    private readonly PetDatabase _database;
    private readonly int _petId;
    private readonly int _eventId;

    private bool _isBusy;
    private bool _finished;

    public HealthEventFormPage(
        PetDatabase database,
        int petId,
        HealthEvent? healthEvent = null)
    {
        InitializeComponent();

        _database = database;
        _petId = petId;
        _eventId = healthEvent?.Id ?? 0;

        TypePicker.SelectedIndex = 0;
        EventDatePicker.Date = DateTime.Today;

        if (healthEvent is not null)
        {
            Title = "Edit Health Event";
            SaveButton.Text = "Save changes";
            DeleteButton.IsVisible = true;

            TitleEntry.Text = healthEvent.Title;
            TypePicker.SelectedIndex = (int)healthEvent.Type;
            EventDatePicker.Date = healthEvent.Date;
            NotesEditor.Text = healthEvent.Notes;
            CompletedSwitch.IsToggled = healthEvent.IsCompleted;
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_isBusy || _finished)
            return;

        string title = TitleEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title))
        {
            ShowError("Enter an event title.");
            TitleEntry.Focus();
            return;
        }

        if (TypePicker.SelectedIndex < 0 ||
            TypePicker.SelectedIndex > 2)
        {
            ShowError("Select an event type.");
            return;
        }

        if (EventDatePicker.Date is not DateTime date)
        {
            ShowError("Select a date.");
            return;
        }

        if (CompletedSwitch.IsToggled && date.Date > DateTime.Today)
        {
            ShowError("A completed event cannot have a future date.");
            return;
        }

        var healthEvent = new HealthEvent
        {
            Id = _eventId,
            PetId = _petId,
            Title = title,
            Type = (HealthEventType)TypePicker.SelectedIndex,
            Date = date.Date,
            Notes = NotesEditor.Text?.Trim() ?? string.Empty,
            IsCompleted = CompletedSwitch.IsToggled
        };

        SetBusy(true);
        ErrorLabel.IsVisible = false;

        try
        {
            int affectedRows =
                await _database.SaveHealthEventAsync(healthEvent);

            if (affectedRows != 1)
            {
                ShowError("Could not save this event. Please try again.");
                return;
            }

            _finished = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            ShowError(
                "Could not save this event. Return to the pet's profile and try again.");

            return;
        }
        finally
        {
            SetBusy(false);
        }

        await ReturnToProfileAsync();
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_isBusy || _finished || _eventId == 0)
            return;

        SetBusy(true);
        ErrorLabel.IsVisible = false;

        try
        {
            bool confirmed = await DisplayAlertAsync(
                "Delete event?",
                "This health event will be permanently deleted.",
                "Delete",
                "Cancel");

            if (!confirmed)
                return;

            await _database.DeleteHealthEventAsync(
                new HealthEvent
                {
                    Id = _eventId,
                    PetId = _petId
                });

            _finished = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ShowError("Could not delete this event. Please try again.");
            return;
        }
        finally
        {
            SetBusy(false);
        }

        await ReturnToProfileAsync();
    }

    private async Task ReturnToProfileAsync()
    {
        try
        {
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ShowError("Your changes were saved. Use Back to return.");
        }
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        FormContent.IsEnabled = !busy && !_finished;
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}