using System.Collections.ObjectModel;
using System.Diagnostics;
using MyPet.Models;
using MyPet.Services;
using MyPet.Views;

namespace MyPet;

public partial class MainPage : ContentPage
{
    private readonly PetDatabase _database = new();
    private List<Pet> _allPets = new();

    private bool _isNavigating;
    private bool _isLoading;
    private string _searchText = string.Empty;

    public ObservableCollection<Pet> Pets { get; } = new();

    public string EmptyTitle { get; private set; } = "Welcome to MyPet!";

    public string EmptyMessage { get; private set; } =
        "Add your first pet and start keeping track of everything.";

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    //Ucitava ljubimce iz baze svaki put kada se pocetna stranica prikaze
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isLoading)
            return;

        _isLoading = true;

        try
        {
            _allPets = await _database.GetPetsAsync();
            ApplySearch();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);

            await DisplayAlertAsync(
                "Error",
                "Could not load your pets. Please try again.",
                "OK");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue?.Trim() ?? string.Empty;
        ApplySearch();
    }

    //Filtrira ljubimce po imenu, vrsti ili rasi
    private void ApplySearch()
    {
        var filteredPets = _allPets.Where(pet =>
            string.IsNullOrWhiteSpace(_searchText) ||
            pet.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
            pet.Species.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
            pet.Breed.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

        if (_allPets.Count == 0)
        {
            EmptyTitle = "Welcome to MyPet!";
            EmptyMessage =
                "Add your first pet and start keeping track of everything.";
        }
        else
        {
            EmptyTitle = "No pets found";
            EmptyMessage = "Try a different name, species or breed.";
        }

        OnPropertyChanged(nameof(EmptyTitle));
        OnPropertyChanged(nameof(EmptyMessage));

        Pets.Clear();

        foreach (var pet in filteredPets)
        {
            Pets.Add(pet);
        }
    }

    private async void OnAddPetClicked(object? sender, EventArgs e)
    {
        await OpenPageAsync(new PetFormPage(_database));
    }

    private async void OnCareOverviewClicked(object? sender, EventArgs e)
    {
        await OpenPageAsync(new CareOverviewPage(_database));
    }

    private async void OnPetSelected(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Pet pet)
            return;

        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }

        await OpenPageAsync(new PetDetailsPage(_database, pet.Id));
    }

    private async Task OpenPageAsync(Page page)
    {
        if (_isNavigating)
            return;

        _isNavigating = true;
        AddPetButton.IsEnabled = false;

        try
        {
            await Navigation.PushAsync(page);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);

            await DisplayAlertAsync(
                "Error",
                "Could not open the page. Please try again.",
                "OK");
        }
        finally
        {
            _isNavigating = false;
            AddPetButton.IsEnabled = true;
        }
    }
}