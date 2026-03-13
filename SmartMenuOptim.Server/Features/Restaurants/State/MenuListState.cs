using SmartMenuOptim.Application.Features.Restaurants.DTOs;
using SmartMenuOptim.Server.Features.Restaurants.Services;
using SmartMenuOptim.Server.State;

namespace SmartMenuOptim.Server.Features.Restaurants.State;

/// <summary>
/// State container for MenuList component.
/// Manages loading, error, data, status toggle, and delete state.
/// </summary>
public class MenuListState : ComponentStateBase<IReadOnlyList<MenuDTO>>
{
    private readonly IMenuClientService _menuService;
    private readonly ILogger<MenuListState> _logger;

    private bool _deleting;
    private MenuDTO? _menuToDelete;
    private bool _showDeleteModal;

    public MenuListState(IMenuClientService menuService, ILogger<MenuListState> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    public IReadOnlyList<MenuDTO>? Menus => Data;
    public bool IsDeleting => _deleting;
    public MenuDTO? MenuToDelete => _menuToDelete;
    public bool ShowDeleteModal => _showDeleteModal;

    public async Task LoadAsync(int restaurantId, CancellationToken cancellationToken = default)
    {
        SetLoading();
        var result = await _menuService.GetByRestaurantIdAsync(restaurantId, cancellationToken);

        if (result.IsSuccess)
        {
            SetData(result.Value ?? (IReadOnlyList<MenuDTO>)[]);
            _logger.LogInformation("Loaded {Count} menus for restaurant {RestaurantId}", Data?.Count ?? 0, restaurantId);
        }
        else
        {
            SetError(result.Error ?? "Failed to load menus.");
        }
    }

    public async Task ToggleStatusAsync(MenuDTO menu, CancellationToken cancellationToken = default)
    {
        var newStatus = !menu.IsActive;
        var result = newStatus
            ? await _menuService.ActivateAsync(menu.Id, cancellationToken)
            : await _menuService.DeactivateAsync(menu.Id, cancellationToken);

        if (result.IsSuccess)
        {
            menu.IsActive = newStatus;
            _logger.LogInformation("Menu {MenuId} status toggled to {Status}", menu.Id, newStatus);
            NotifyStateChanged();
        }
        else
        {
            SetError(result.Error ?? "Failed to update menu status.");
        }
    }

    public void ConfirmDelete(MenuDTO menu)
    {
        _menuToDelete = menu;
        _showDeleteModal = true;
        NotifyStateChanged();
    }

    public void CancelDelete()
    {
        _menuToDelete = null;
        _showDeleteModal = false;
        NotifyStateChanged();
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (_menuToDelete is null) return;
        _deleting = true;
        NotifyStateChanged();

        var result = await _menuService.DeleteAsync(_menuToDelete.Id, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Menu {MenuId} deleted", _menuToDelete.Id);
            if (Data is not null)
                SetData(Data.Where(m => m.Id != _menuToDelete.Id).ToList());
            CancelDelete();
        }
        else
        {
            SetError(result.Error ?? "Failed to delete menu.");
        }

        _deleting = false;
        NotifyStateChanged();
    }

    public void ClearError() { if (HasError) SetError(null!); }
}
