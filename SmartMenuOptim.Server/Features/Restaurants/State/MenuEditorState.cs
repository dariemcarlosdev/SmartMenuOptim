using SmartMenuOptim.Application.Dtos.Restaurant;
using SmartMenuOptim.Server.Features.Restaurants.Services;
using SmartMenuOptim.Server.State;

namespace SmartMenuOptim.Server.Features.Restaurants.State;

/// <summary>
/// State container for MenuEditor component.
/// Manages loading, saving, and availability state for menu create/edit.
/// </summary>
public class MenuEditorState : ComponentStateBase<MenuDTO>
{
    private readonly IMenuClientService _menuService;
    private readonly ILogger<MenuEditorState> _logger;

    private bool _saving;

    public MenuEditorState(IMenuClientService menuService, ILogger<MenuEditorState> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    public MenuDTO? Menu => Data;
    public bool IsSaving { get => _saving; private set { _saving = value; NotifyStateChanged(); } }

    public async Task LoadAsync(int menuId, CancellationToken cancellationToken = default)
    {
        SetLoading();
        var result = await _menuService.GetByIdAsync(menuId, cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            SetData(result.Value);
            _logger.LogInformation("Loaded menu {MenuId} for editing", menuId);
        }
        else
        {
            SetError(result.Error ?? "Menu not found.");
        }
    }

    public async Task<bool> CreateAsync(int restaurantId, MenuCreateDTO dto, CancellationToken cancellationToken = default)
    {
        IsSaving = true;

        var result = await _menuService.CreateAsync(restaurantId, dto, cancellationToken);

        IsSaving = false;

        if (result.IsSuccess)
        {
            _logger.LogInformation("Menu created successfully");
            return true;
        }

        SetError(result.Error ?? "Failed to create menu.");
        return false;
    }

    public async Task<bool> UpdateAsync(MenuUpdateDTO dto, CancellationToken cancellationToken = default)
    {
        IsSaving = true;

        var result = await _menuService.UpdateAsync(dto, cancellationToken);

        IsSaving = false;

        if (result.IsSuccess)
        {
            _logger.LogInformation("Menu {MenuId} updated successfully", dto.Id);
            return true;
        }

        SetError(result.Error ?? "Failed to update menu.");
        return false;
    }

    public void ClearError() { if (HasError) SetError(null!); }
}
