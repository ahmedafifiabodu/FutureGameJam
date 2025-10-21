using UnityEngine;
using UnityEngine.Pool;
using AI.Enemy;
using AI.Enemy.Configuration;

public class ProjectilePooling : MonoBehaviour
{
    [SerializeField] private int defultPoolSize = 10;
    [SerializeField] private int maxPoolSize = 10;
    [SerializeField] private Transform bulletPoolParent;
    [SerializeField] internal Transform shootPoint;
    [SerializeField] private EnemyController enemyController;

    private Transform player;

    private void Awake()
    {
        if (enemyController == null)
        {
            enemyController = GetComponent<EnemyController>();
        }

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag(GameConstant.Tags.Player);
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    internal ObjectPool<Projectile> ProjectilePool { get; private set; }

    private void Start()
    {
        if (bulletPoolParent == null)
        {
            bulletPoolParent = new GameObject("Projectile Pool").transform;
            bulletPoolParent.SetParent(transform);
        }

        ProjectilePool = new ObjectPool<Projectile>(
                    CreateProjectile,
                 OnGetProjectleFromPool,
              OnReleaseProjectileToPool,
               OnDestroyProjectile,
              true,
                defultPoolSize,
        maxPoolSize
              );
    }

    private Projectile CreateProjectile()
    {
        if (enemyController == null || enemyController.Config == null || enemyController.Config.projectilePrefab == null)
        {
            Debug.LogError("[ProjectilePooling] Missing projectile prefab in enemy config!");
            return null;
        }

        GameObject projectileObj = Instantiate(enemyController.Config.projectilePrefab, shootPoint.position, Quaternion.identity, bulletPoolParent);
        Projectile projectile = projectileObj.GetComponent<Projectile>();

        if (projectile == null)
        {
            Debug.LogError("[ProjectilePooling] Projectile prefab missing Projectile component!");
            Destroy(projectileObj);
            return null;
        }

        projectile.SetPool(ProjectilePool);
        return projectile;
    }

    private void OnGetProjectleFromPool(Projectile projectile)
    {
        if (projectile == null || shootPoint == null) return;

        projectile.transform.position = shootPoint.position;
        projectile.transform.right = shootPoint.right;
        projectile.gameObject.SetActive(true);

        if (player != null && projectile.re != null)
        {
            Vector3 direction = (player.position - shootPoint.position).normalized;
            float speed = enemyController != null && enemyController.Config != null
                       ? enemyController.Config.projectileSpeed
                : 20f;
            projectile.re.linearVelocity = direction * speed;
        }
    }

    private void OnReleaseProjectileToPool(Projectile projectile)
    {
        if (projectile != null && projectile.gameObject != null)
        {
            projectile.gameObject.SetActive(false);
        }
    }

    private void OnDestroyProjectile(Projectile projectile)
    {
        if (projectile != null && projectile.gameObject != null)
        {
            Destroy(projectile.gameObject);
        }
    }
}