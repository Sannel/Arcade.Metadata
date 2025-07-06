using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Sannel.Arcade.Metadata.Client.Services;
using Sannel.Arcade.Metadata.Client.Models;
using MudBlazor;
using Sannel.Arcade.Metadata.Client.Components.Dialog;

namespace Sannel.Arcade.Metadata.Client.Pages;

[Authorize]
public partial class Games : ComponentBase
{
	[Inject] private IMetadataService MetadataService { get; set; } = null!;
	[Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
	[Inject] private NavigationManager Navigation { get; set; } = null!;

	[Parameter] public string? Platform { get; set; }

	[Inject]
	private IDialogService DialogService { get; set; } = default!;

	private GetGamesResponse _gamesResponse = new();
	private string _errorMessage = string.Empty;
	private bool _isLoading = false;
	private List<GameMetadata> _filteredGames = [];
	private string _searchText = string.Empty;
	private HashSet<string> _failedImages = new();

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
	/// Opens the game details dialog for the specified game.
	/// </summary>
	/// <param name="game">The game metadata to display.</param>
	private async Task NavigateToGameDetails(GameMetadata game)
	{
		if (string.IsNullOrEmpty(game.Id) || string.IsNullOrEmpty(Platform))
		{
			return; // Can't open dialog without required parameters
		}

		var parameters = new DialogParameters<GameInfo>()
		{
			{ x => x.GameId, game.Id },
			{ x => x.PlatformName, Platform }, // Use the page's Platform parameter
		};

		var options = new DialogOptions()
		{
			MaxWidth = MaxWidth.ExtraLarge,
			FullWidth = true,
			CloseButton = true,
			CloseOnEscapeKey = true,
			BackdropClick = false // Prevent accidental closing
		};

		await DialogService.ShowAsync<GameInfo>(game.Name ?? "Game Information", parameters, options);
	}

	/// <summary>
	/// Handles image loading errors by adding the game ID to the failed images set.
	/// </summary>
	/// <param name="game">The game metadata to update.</param>
	private void HandleCoverImageError(GameMetadata game)
	{
		// Add game ID to failed images set to prevent retry and show icon instead
		if (!string.IsNullOrEmpty(game.Id))
		{
			_failedImages.Add(game.Id);
		}
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