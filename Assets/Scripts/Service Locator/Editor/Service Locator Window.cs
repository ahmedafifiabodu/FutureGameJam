using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ServiceLocatorWindow : EditorWindow
{
    private readonly List<(object service, string reason, string caller)> unregisteredServices = new();
    private readonly List<object> registeredServices = new();

    [MenuItem("System/Service Locator")]
    public static void ShowWindow()
    {
        GetWindow<ServiceLocatorWindow>("Service Locator");
    }

    private void OnEnable()
    {
        ServiceLocator.Instance.ServiceRegistered += OnServiceRegistered;
        ServiceLocator.Instance.ServiceUnregistered += OnServiceUnregistered;
    }

    private void OnDisable()
    {
        ServiceLocator.Instance.ServiceRegistered -= OnServiceRegistered;
        ServiceLocator.Instance.ServiceUnregistered -= OnServiceUnregistered;
    }

    private void OnServiceRegistered(object service)
    {
        registeredServices.Add(service);
        Repaint();
    }

    private void OnServiceUnregistered(object service, string reason, string caller)
    {
        registeredServices.Remove(service);
        unregisteredServices.Add((service, reason, caller));
        Repaint();
    }

    private void OnGUI()
    {
        GUILayout.Space(20);

        // Center the "Registered Services" label
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("Registered Services", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        // Add a space between the label and the list of services
        GUILayout.Space(10);

        // Display the services in the order they were registered and number them
        if (registeredServices.Count == 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("No Service Registered");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        else
        {
            for (int i = 0; i < registeredServices.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label($"{i + 1}. {registeredServices[i].GetType().Name}");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
        }

        // Add a space between the registered and unregistered services
        GUILayout.Space(20);

        // Center the "Unregistered Services" label
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("Unregistered Services", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        // Add a space between the label and the list of unregistered services
        GUILayout.Space(10);

        // Display the unregistered services in the order they were unregistered and number them
        if (unregisteredServices.Count == 0)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("No Unregistered Services");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        else
        {
            for (int i = 0; i < unregisteredServices.Count; i++)
            {
                var (service, reason, caller) = unregisteredServices[i];
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label($"{i + 1}. {service.GetType().Name}");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Reason: {reason}");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label($"Caller: {caller}");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(10); // Add space between each unregistered service entry
            }
        }
    }
}