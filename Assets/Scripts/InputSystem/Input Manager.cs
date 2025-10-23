using UnityEngine;

public class InputManager : MonoBehaviour
{
    private InputSystem_Actions _gameInputSystem;
    internal InputSystem_Actions.HumanActions PlayerActions { get; private set; }
    internal InputSystem_Actions.ParasiteActions ParasiteActions { get; private set; }
    internal InputSystem_Actions.UIActions UIActions { get; private set; }

    private ServiceLocator _serviceLocator;

    private void Awake()
    {
        _serviceLocator = ServiceLocator.Instance;
        _serviceLocator.RegisterService(this, true);

        _gameInputSystem = new InputSystem_Actions();

        InitializeActions();
    }

    private void OnEnable() => _gameInputSystem.Enable();

    private void OnDisable() => _gameInputSystem?.Disable();

    private void InitializeActions()
    {
        PlayerActions = _gameInputSystem.Human;
        ParasiteActions = _gameInputSystem.Parasite;
        UIActions = _gameInputSystem.UI;

        // Always keep UI actions enabled for pause functionality
        UIActions.Enable();
    }

    public void EnablePlayerActions()
    {
        PlayerActions.Enable();
        // Keep Parasite actions enabled for the Exit For Host action
        // This allows checking the exit button while controlling a host
        ParasiteActions.Enable();
        UIActions.Enable();
    }

    public void EnableParasiteActions()
    {
        ParasiteActions.Enable();
        PlayerActions.Disable();
        UIActions.Enable();
    }

    public void DisableAllActions()
    {
        PlayerActions.Disable();
        ParasiteActions.Disable();
        // Keep UI actions enabled even when gameplay is disabled
        UIActions.Enable();
    }
}