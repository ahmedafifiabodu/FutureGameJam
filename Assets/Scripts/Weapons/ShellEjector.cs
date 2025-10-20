using UnityEngine;

/// <summary>
/// Simple shell ejection system for weapons.
/// Spawns bullet casings that are ejected when shooting.
/// </summary>
public class ShellEjector : MonoBehaviour
{
    [Header("Shell Settings")]
    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private Transform ejectionPoint;
    [SerializeField] private float ejectionForce = 5f;
    [SerializeField] private float ejectionTorque = 10f;
    [SerializeField] private float shellLifetime = 5f;

    [Header("Ejection Direction")]
    [SerializeField] private Vector3 ejectionDirection = new Vector3(1f, 1f, 0f);
    [SerializeField] private float ejectionSpread = 15f;

    public void EjectShell()
    {
        if (!shellPrefab || !ejectionPoint) return;

        // Spawn shell
        GameObject shell = Instantiate(shellPrefab, ejectionPoint.position, ejectionPoint.rotation);

        // Add physics
        Rigidbody rb = shell.GetComponent<Rigidbody>();
        if (rb)
        {
            // Calculate ejection direction with spread
            Vector3 direction = ejectionPoint.TransformDirection(ejectionDirection);
            direction += Random.insideUnitSphere * ejectionSpread * 0.1f;
            direction.Normalize();

            rb.linearVelocity = direction * ejectionForce;
            rb.angularVelocity = Random.insideUnitSphere * ejectionTorque;
        }

        // Destroy after lifetime
        Destroy(shell, shellLifetime);
    }

    private void OnDrawGizmosSelected()
    {
        if (!ejectionPoint) return;

        Gizmos.color = Color.yellow;
        Vector3 direction = ejectionPoint.TransformDirection(ejectionDirection).normalized;
        Gizmos.DrawRay(ejectionPoint.position, direction * 0.5f);
    }
}
