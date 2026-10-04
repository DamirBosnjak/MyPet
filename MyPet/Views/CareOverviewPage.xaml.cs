using System.Collections.ObjectModel;
using MyPet.Models;
using MyPet.Services;

namespace MyPet.Views;

public partial class CareOverviewPage : ContentPage
{
    private readonly PetDatabase _database;

    private readonly ObservableCollection<CareItem> _visibleItems = new();

    private List<Pet> _pets = new();
    private List<CareItem> _allItems = new();

    private bool _isLoading;
    private bool _isNavigating;

    public CareOverviewPage(PetDatabase database)
    {
        InitializeComponent();

        _database = database;

        EventsCollection.ItemsSource = _visibleItems;

        PetPicker.Items.Add("All pets");
        PetPicker.SelectedIndex = 0;

        StatusPicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadEventsAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await LoadEventsAsync();
    }

    private async Task LoadEventsAsync()
    {
        if (_isLoading || _isNavigating)
            return;

        int? selectedPetId = GetSelectedPetId();

        _isLoading = true;
        UpdateControls();
        SummaryLabel.Text = "Loading events...";

        try
        {
            var pets = await _database.GetPetsAsync();
            var events = await _database.GetAllHealthEventsAsync();

            _pets = pets
                .OrderBy(pet => pet.Name)
                .ToList();

            var petNames = _pets.ToDictionary(
                pet => pet.Id,
                pet => pet.Name);

            _allItems = events
                .Where(healthEvent =>
                    petNames.ContainsKey(healthEvent.PetId))
                .Select(healthEvent => new CareItem
                {
                    Event = healthEvent,
                    PetName = petNames[healthEvent.PetId]
                })
                .ToList();

            PetPicker.Items.Clear();
            PetPicker.Items.Add("All pets");

            foreach (var pet in _pets)
            {
                PetPicker.Items.Add($"{pet.Name} ({pet.Species})");
            }

            int previousIndex = _pets.FindIndex(
                pet => pet.Id == selectedPetId);

            PetPicker.SelectedIndex =
                previousIndex >= 0 ? previousIndex + 1 : 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            _allItems.Clear();
            _visibleItems.Clear();
            SummaryLabel.Text = "Could not load events. Tap Refresh.";

            await DisplayAlertAsync(
                "Error",
                "Could not load health events. Please try Refresh.",
                "OK");

            return;
        }
        finally
        {
            _isLoading = false;
            UpdateControls();
        }

        ApplyFilters();
    }

    private int? GetSelectedPetId()
    {
        int index = PetPicker.SelectedIndex - 1;

        if (index >= 0 && index < _pets.Count)
            return _pets[index].Id;

        return null;
    }

    private void OnFilterChanged(object? sender, EventArgs e)
    {
        if (_isLoading)
            return;

        ApplyFilters();
    }

    private void ApplyFilters()
    {
        IEnumerable<CareItem> results = _allItems;

        int? petId = GetSelectedPetId();

        if (petId.HasValue)
        {
            results = results.Where(
                item => item.Event.PetId == petId.Value);
        }

        results = StatusPicker.SelectedIndex switch
        {
            0 => results.Where(item => !item.Event.IsCompleted),
            1 => results.Where(item => item.Event.IsCompleted),
            2 => results.Where(item => item.Event.IsOverdue),
            _ => results
        };

        var filteredItems = results
            .OrderBy(item => item.Event.Date)
            .ThenBy(item => item.PetName)
            .ToList();

        _visibleItems.Clear();

        foreach (var item in filteredItems)
        {
            _visibleItems.Add(item);
        }

        int count = filteredItems.Count;

        SummaryLabel.Text = count == 1
            ? "1 event"
            : $"{count} events";
    }

    private async void OnEventSelected(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not CareItem item)
            return;

        EventsCollection.SelectedItem = null;

        if (_isLoading || _isNavigating)
            return;

        _isNavigating = true;
        UpdateControls();

        try
        {
            await Navigation.PushAsync(
                new HealthEventFormPage(
                    _database,
                    item.Event.PetId,
                    item.Event));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await DisplayAlertAsync(
                "Error",
                "Could not open this event. Please try again.",
                "OK");
        }
        finally
        {
            _isNavigating = false;
            UpdateControls();
        }
    }

    private void UpdateControls()
    {
        bool enabled = !_isLoading && !_isNavigating;

        PetPicker.IsEnabled = enabled;
        StatusPicker.IsEnabled = enabled;
        EventsCollection.IsEnabled = enabled;

        LoadingIndicator.IsVisible = _isLoading;
        LoadingIndicator.IsRunning = _isLoading;
    }
}