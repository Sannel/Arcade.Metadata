using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Sannel.Arcade.Metadata.Client.Services;
using Sannel.Arcade.Metadata.Client.Models;

namespace Sannel.Arcade.Metadata.Client.Pages;

[Authorize]
public partial class Games : ComponentBase
{
	[Inject] private IMetadataService MetadataService { get; set; } = null!;
	[Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
	[Inject] private NavigationManager Navigation { get; set; } = null!;

	[Parameter] public string? Platform { get; set; }

	private GetGamesResponse _gamesResponse = new();
	private string _errorMessage = string.Empty;
	private bool _isLoading = false;
	private List<GameMetadata> _filteredGames = [];
	private string _searchText = string.Empty;

	protected override async Task OnInitializedAsync()
	{
		var state = await AuthenticationStateProvider.GetAuthenticationStateAsync();
		await LoadGames();
	}

	protected override async Task OnParametersSetAsync()
	{
		await LoadGames();
	}

	private async Task LoadGames()
	{
		if (string.IsNullOrWhiteSpace(Platform))
		{
			Navigation.NavigateTo("/collection");
			return;
		}

		_isLoading = true;
		_errorMessage = string.Empty;
		StateHasChanged();

		try
		{
			_gamesResponse = await MetadataService.GetGamesAsync(Platform);
			if (!_gamesResponse.Success)
			{
				_errorMessage = _gamesResponse.Message;
			}
			else
			{
				_filteredGames = _gamesResponse.Games;
			}
		}
		catch (Exception ex)
		{
			_errorMessage = $"Error loading games: {ex.Message}";
		}
		finally
		{
			_isLoading = false;
			StateHasChanged();
		}
	}

	private void FilterGames()
	{
		if (string.IsNullOrWhiteSpace(_searchText))
		{
			_filteredGames = _gamesResponse.Games;
		}
		else
		{
			_filteredGames = _gamesResponse.Games
				.Where(g => g.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
						   (g.Description?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
						   g.Genres.Any(genre => genre.Contains(_searchText, StringComparison.OrdinalIgnoreCase)))
				.ToList();
		}
		StateHasChanged();
	}

	private void OnSearchTextChanged(string searchText)
	{
		_searchText = searchText;
		FilterGames();
	}

	private void BackToCollection()
	{
		Navigation.NavigateTo("/collection");
	}

	/// <summary>
	/// Navigates to the game details page using the game ID.
	/// </summary>
	/// <param name="game">The game metadata containing the ID.</param>
	private void NavigateToGameDetails(GameMetadata game)
	{
		if (!string.IsNullOrEmpty(game.Id) && !string.IsNullOrEmpty(Platform))
		{
			Navigation.NavigateTo($"/games/{Platform}/{game.Id}");
		}
	}

	/// <summary>
	/// Handles image loading errors by clearing the image URL to prevent broken image displays.
	/// </summary>
	/// <param name="game">The game metadata to update.</param>
	private void HandleCoverImageError(GameMetadata game)
	{
		// Clear the cover URL to prevent showing broken image
		game.CoverUrl = null;
		StateHasChanged();
	}

	/// <summary>
	/// Gets the ROM file name without extension for display purposes.
	/// </summary>
	/// <param name="game">The game metadata containing the ROM file name.</param>
	/// <returns>The ROM file name without extension, or an empty string if not available.</returns>
	private static string GetRomFileNameWithoutExtension(GameMetadata game)
	{
		if (string.IsNullOrEmpty(game.RomFileName))
			return string.Empty;

		return Path.GetFileNameWithoutExtension(game.RomFileName);
	}
}