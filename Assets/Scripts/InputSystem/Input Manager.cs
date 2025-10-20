using UnityEngine;

public class InputManager : MonoBehaviour
{
    private InputSystem_Actions _gameInputSystem;
    internal InputSystem_Actions.PlayerActions PlayerActions { get; private set; }

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
        PlayerActions = _gameInputSystem.Player;
    }
}