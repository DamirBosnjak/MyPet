using System.Collections.ObjectModel;
using MyPet.Models;
using MyPet.Services;

namespace MyPet.Views;

public partial class PetDetailsPage : ContentPage
{
    private readonly PetDatabase _database;
    private readonly int _petId;

    private readonly ObservableCollection<HealthEvent> _events = new();

    private Pet? _pet;
    private bool _isBusy;
    private bool _isLoading;

    public PetDetailsPage(PetDatabase database, int petId)
    {
        InitializeComponent();

        _database = database;
        _petId = petId;

        EventsCollection.ItemsSource = _events;
    }

    //Ucitava podatke o ljubimcu i njegove zdravstvene dogadjaje
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isLoading)
            return;

        _isLoading = true;
        _pet = null;
        UpdateButtons();

        try
        {
            var pet = await _database.GetPetAsync(_petId);

            if (pet is null)
            {
                BindingContext = null;
                _events.Clear();

                await DisplayAlertAsync(
                    "Pet not found",
                    "This pet is no longer available. Return to My Pets.",
                    "OK");

                return;
            }

            var events = await _database.GetHealthEventsAsync(_petId);

            _pet = pet;
            BindingContext = _pet;

            _events.Clear();

            foreach (var healthEvent in events)
            {
                _events.Add(healthEvent);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await DisplayAlertAsync(
                "Error",
                "Could not load this profile. Return to My Pets and try again.",
                "OK");
        }
        finally
        {
            _isLoading = false;
            UpdateButtons();
        }
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (_pet is null)
            return;

        await OpenPageAsync(new PetFormPage(_database, _pet));
    }

    private async void OnAddEventClicked(object? sender, EventArgs e)
    {
        await OpenPageAsync(
            new HealthEventFormPage(_database, _petId));
    }

    private async void OnEventSelected(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not HealthEvent healthEvent)
            return;

        EventsCollection.SelectedItem = null;

        await OpenPageAsync(
            new HealthEventFormPage(_database, _petId, healthEvent));
    }

    private async Task OpenPageAsync(Page page)
    {
        if (_isBusy || _isLoading || _pet is null)
            return;

        _isBusy = true;
        UpdateButtons();

        try
        {
            await Navigation.PushAsync(page);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await DisplayAlertAsync(
                "Error",
                "Could not open this page. Please try again.",
                "OK");
        }
        finally
        {
            _isBusy = false;
            UpdateButtons();
        }
    }

    //Trazi potvrdu korisnika pre brisanja ljubimca i povezanih podataka
    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (_isBusy || _isLoading || _pet is null)
            return;

        _isBusy = true;
        UpdateButtons();

        bool deleted = false;

        try
        {
            bool confirmed = await DisplayAlertAsync(
                "Delete pet?",
                $"Delete {_pet.Name} and all associated health events? This cannot be undone.",
                "Delete",
                "Cancel");

            if (!confirmed)
                return;

            await _database.DeletePetAsync(_pet);

            deleted = true;
            _pet = null;

            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            string message = deleted
                ? "Your pet was deleted. Use Back to return to My Pets."
                : "Could not delete your pet. Please try again.";

            await DisplayAlertAsync("Notice", message, "OK");
        }
        finally
        {
            _isBusy = false;
            UpdateButtons();
        }
    }

    private void UpdateButtons()
    {
        bool enabled = _pet is not null
            && !_isBusy
            && !_isLoading;

        EditButton.IsEnabled = enabled;
        DeleteButton.IsEnabled = enabled;
        AddEventButton.IsEnabled = enabled;
        EventsCollection.IsEnabled = enabled;
    }
}