using UnityEngine;

public class InputManager : MonoBehaviour
{
    private InputSystem_Actions _gameInputSystem;
    internal InputSystem_Actions.HumanActions PlayerActions { get; private set; }
    internal InputSystem_Actions.ParasiteActions ParasiteActions { get; private set; }

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
    }

    public void EnablePlayerActions()
    {
        PlayerActions.Enable();
        ParasiteActions.Disable();
    }

    public void EnableParasiteActions()
    {
        ParasiteActions.Enable();
        PlayerActions.Disable();
    }

    public void DisableAllActions()
    {
        PlayerActions.Disable();
        ParasiteActions.Disable();
    }
}