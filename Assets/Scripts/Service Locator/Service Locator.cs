using System;
using System.Collections.Generic;
using System.Diagnostics;

public class ServiceLocator
{
    public static ServiceLocator Instance => _instance ??= new ServiceLocator();

    private ServiceLocator() => UnityEngine.SceneManagement.SceneManager.sceneUnloaded += scene => UnregisterNonPersistentServices();

    #region Properties

    private static ServiceLocator _instance;
    private readonly IDictionary<object, (object service, bool dontDestroyOnLoad)> services = new Dictionary<object, (object service, bool dontDestroyOnLoad)>();

#if UNITY_EDITOR
    private readonly List<(object service, string reason, string caller)> unregisteredServices = new();
#endif

    #endregion Properties

    #region Service Events

    // Event to notify when a service is registered
    public event Action<object> ServiceRegistered;

    // Event to notify when a service is unregistered
    public event Action<object, string, string> ServiceUnregistered;

    #endregion Service Events

    #region Service Registration

    // Register a service
    public void RegisterService<T>(T service, bool dontDestroyOnLoad)
    {
        Type serviceType = typeof(T);

        // Check if the service is already registered
        if (services.ContainsKey(serviceType))
        {
            Logging.LogWarning($"Service of type {serviceType.Name} is already registered.");

            if (service is UnityEngine.Component existingComponent)
                UnityEngine.Object.Destroy(existingComponent.gameObject);
            else if (service is UnityEngine.GameObject existingGameObject)
                UnityEngine.Object.Destroy(existingGameObject);

            return;
        }

        // If the service is a Component or GameObject, remove its parent and set it to DontDestroyOnLoad if necessary
        if (service is UnityEngine.Component newComponent)
        {
            if (newComponent.transform.parent != null)
            {
                newComponent.transform.parent = null;

                if (dontDestroyOnLoad)
                    UnityEngine.Object.DontDestroyOnLoad(newComponent.gameObject);
            }
        }
        else if (service is UnityEngine.GameObject newGameObject)
        {
            if (newGameObject.transform.parent != null)
            {
                newGameObject.transform.parent = null;

                if (dontDestroyOnLoad)
                    UnityEngine.Object.DontDestroyOnLoad(newGameObject);
            }
        }

        // Register the service
        services[typeof(T)] = (service, dontDestroyOnLoad);
        ServiceRegistered?.Invoke(service);
    }

    #endregion Service Registration

    #region Service Unregistration

    // Unregister all services that are not marked as DontDestroyOnLoad
    private void UnregisterNonPersistentServices()
    {
        foreach (var service in new List<object>(services.Keys))
        {
            if (!services[service].dontDestroyOnLoad)
            {
                var unregisteredService = services[service].service;
                services.Remove(service);
                string caller = GetCaller();
#if UNITY_EDITOR
                unregisteredServices.Add((unregisteredService, "Unregistered by system", caller));
#endif
                ServiceUnregistered?.Invoke(unregisteredService, "Unregistered by system", caller);
            }
        }
    }

    // Unregister a service by type
    public void UnregisterService<T>()
    {
        Type serviceType = typeof(T);

        if (services.ContainsKey(serviceType))
        {
            var unregisteredService = services[serviceType].service;
            services.Remove(serviceType);
            string caller = GetCaller();
#if UNITY_EDITOR
            unregisteredServices.Add((unregisteredService, "Unregistered by user", caller));
#endif
            ServiceUnregistered?.Invoke(unregisteredService, "Unregistered by user", caller);
        }
        else
        {
            Logging.LogWarning($"Attempted to unregister service of type {serviceType.Name}, but it was not registered.");
        }
    }

    #endregion Service Unregistration

    #region Service Access

    // Get a service by type
    public T GetService<T>()
    {
        if (services.TryGetValue(typeof(T), out var serviceTuple))
            return (T)serviceTuple.service;
        else
            throw new ApplicationException("The requested service is not registered");
    }

    // Try to get a service by type
    public bool TryGetService<T>(out T service)
    {
        service = default;
        if (services.TryGetValue(typeof(T), out var serviceTuple))
        {
            service = (T)serviceTuple.service;
            return true;
        }
        else
            return false;
    }

    // Get all services that are marked as DontDestroyOnLoad
    public List<object> GetDontDestroyOnLoadServices()
    {
        List<object> dontDestroyOnLoadServices = new();

        foreach (var (service, dontDestroyOnLoad) in services.Values)
        {
            if (dontDestroyOnLoad)
                dontDestroyOnLoadServices.Add(service);
        }

        return dontDestroyOnLoadServices;
    }

    // Get all services
    public List<object> GetAllServices()
    {
        List<object> allServices = new();

        foreach (var (service, _) in services.Values)
        {
            allServices.Add(service);
        }

        return allServices;
    }

    #endregion Service Access

    #region Helper Methods

#if UNITY_EDITOR

    // Get all unregistered services
    public List<(object service, string reason, string caller)> GetAllUnregisteredServices() => new(unregisteredServices);

    // Get the caller's information
    private string GetCaller()
    {
        var stackTrace = new StackTrace();

        foreach (var frame in stackTrace.GetFrames())
        {
            var method = frame.GetMethod();

            if (method.DeclaringType != typeof(ServiceLocator))
                return $"{method.DeclaringType.FullName}.{method.Name}";
        }

        return "Unknown Caller";
    }

#endif

    #endregion Helper Methods
}