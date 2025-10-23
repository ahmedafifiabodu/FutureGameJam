using UnityEngine;

/// <summary>
/// Centralized cursor management system.
/// Handles cursor visibility and lock state based on UI panels and game state.
/// </summary>
public class CursorManager : MonoBehaviour
{
    private int _uiPanelsOpen = 0;
    private bool _isGamePaused = false;

    private void Awake()
    {
        ServiceLocator.Instance.RegisterService(this, false);

        // Initialize with cursor locked (game mode)
        SetCursorState(false);
    }

    /// <summary>
    /// Call this when a UI panel is opened.
    /// </summary>
    public void OnUIPanelOpened()
    {
        _uiPanelsOpen++;
        UpdateCursorState();
    }

    /// <summary>
    /// Call this when a UI panel is closed.
    /// </summary>
    public void OnUIPanelClosed()
    {
        _uiPanelsOpen = Mathf.Max(0, _uiPanelsOpen - 1);
        UpdateCursorState();
    }

    /// <summary>
    /// Set whether the game is paused (e.g., pause menu).
    /// </summary>
    public void SetGamePaused(bool isPaused)
    {
        _isGamePaused = isPaused;
        UpdateCursorState();
    }

    /// <summary>
    /// Force cursor state (for special cases).
    /// </summary>
    public void SetCursorState(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void UpdateCursorState()
    {
        // Show cursor if any UI panel is open or game is paused
        bool shouldShowCursor = _uiPanelsOpen > 0 || _isGamePaused;
        SetCursorState(shouldShowCursor);
    }

    /// <summary>
    /// Get current cursor state.
    /// </summary>
    public bool IsCursorVisible() => Cursor.visible;

    /// <summary>
    /// Get number of open UI panels.
    /// </summary>
    public int GetOpenPanelCount() => _uiPanelsOpen;

    private void OnApplicationFocus(bool hasFocus)
    {
        // Re-apply cursor state when window regains focus
        if (hasFocus)
        {
            UpdateCursorState();
        }
    }
}