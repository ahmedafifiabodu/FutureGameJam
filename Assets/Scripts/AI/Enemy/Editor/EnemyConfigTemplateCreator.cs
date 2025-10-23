using UnityEngine;
using UnityEditor;
using AI.Enemy.Configuration;
using System.IO;

namespace AI.Enemy.Editor
{
    /// <summary>
    /// Creates template enemy configurations for quick setup
    /// </summary>
    public static class EnemyConfigTemplateCreator
    {
        private const string CONFIG_PATH = "Assets/ScriptableObjects/EnemyConfigs";

        [MenuItem("Tools/AI/Create Template Enemy Configs")]
        public static void CreateTemplateConfigs()
        {
            // Ensure directory exists
            if (!Directory.Exists(CONFIG_PATH))
            {
                Directory.CreateDirectory(CONFIG_PATH);
            }

            CreateBasicEnemyTemplate();
            CreateToughEnemyTemplate();
            CreateFastEnemyTemplate();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created template enemy configs at: {CONFIG_PATH}");
        }

        private static void CreateBasicEnemyTemplate()
        {
            string path = $"{CONFIG_PATH}/BasicEnemy_Template.asset";
            if (File.Exists(path))
            {
                Debug.Log("Basic Enemy template already exists, skipping...");
                return;
            }

            EnemyConfigSO config = ScriptableObject.CreateInstance<EnemyConfigSO>();

            // Identity
            config.enemyName = "Basic Soldier";
            config.enemyType = EnemyType.Basic;

            // Stats
            config.maxHealth = 50f;
            config.attackDamage = 15;
            config.moveSpeed = 3f;

            // Patrol settings
            config.patrolSpeed = 2f;
            config.patrolWaitTime = 2f;
            config.usePatrolPoints = true;

            // Vision settings
            config.sightRange = 12f;
            config.fieldOfViewAngle = 90f;
            config.visionCheckInterval = 0.5f;

            // Chase settings
            config.chaseSpeed = 4f;
            config.chaseLostSightTime = 3f;

            // Combat
            config.attackRange = 2f;
            config.attackCooldown = 2f;

            // Stagger
            config.staggerChance = 0.3f;
            config.staggerDuration = 0.5f;

            // Spawning
            config.spawnWeight = 50;
            config.minRoomIteration = 0;

            // Type-specific
            config.memoryDelay = 1f;

            AssetDatabase.CreateAsset(config, path);
            Debug.Log($"Created Basic Enemy template at {path}");
        }

        private static void CreateToughEnemyTemplate()
        {
            string path = $"{CONFIG_PATH}/ToughEnemy_Template.asset";
            if (File.Exists(path))
            {
                Debug.Log("Tough Enemy template already exists, skipping...");
                return;
            }

            EnemyConfigSO config = ScriptableObject.CreateInstance<EnemyConfigSO>();

            // Identity
            config.enemyName = "Tough Shotgunner";
            config.enemyType = EnemyType.Tough;

            // Stats
            config.maxHealth = 100f;
            config.attackDamage = 25;
            config.moveSpeed = 2.5f;

            // Patrol settings
            config.patrolSpeed = 2f;
            config.patrolWaitTime = 2f;
            config.usePatrolPoints = true;

            // Vision settings
            config.sightRange = 12f;
            config.fieldOfViewAngle = 90f;
            config.visionCheckInterval = 0.5f;

            // Chase settings
            config.chaseSpeed = 4f;
            config.chaseLostSightTime = 3f;

            // Combat
            config.attackRange = 8f;
            config.attackCooldown = 3f;

            // Stagger
            config.staggerChance = 0.2f;
            config.staggerDuration = 0.3f;

            // Spawning
            config.spawnWeight = 30;
            config.minRoomIteration = 2;

            // Type-specific
            config.shotgunConeAngle = 30f;
            config.shotgunRange = 8f;
            config.aimDuration = 1.5f;

            AssetDatabase.CreateAsset(config, path);
            Debug.Log($"Created Tough Enemy template at {path}");
        }

        private static void CreateFastEnemyTemplate()
        {
            string path = $"{CONFIG_PATH}/FastEnemy_Template.asset";
            if (File.Exists(path))
            {
                Debug.Log("Fast Enemy template already exists, skipping...");
                return;
            }

            EnemyConfigSO config = ScriptableObject.CreateInstance<EnemyConfigSO>();

            // Identity
            config.enemyName = "Fast Assassin";
            config.enemyType = EnemyType.Fast;

            // Stats
            config.maxHealth = 30f;
            config.attackDamage = 20;
            config.moveSpeed = 5f;

            // Patrol settings
            config.patrolSpeed = 2f;
            config.patrolWaitTime = 2f;
            config.usePatrolPoints = true;

            // Vision settings
            config.sightRange = 12f;
            config.fieldOfViewAngle = 90f;
            config.visionCheckInterval = 0.5f;

            // Chase settings
            config.chaseSpeed = 4f;
            config.chaseLostSightTime = 3f;

            // Combat
            config.attackRange = 1.5f;
            config.attackCooldown = 1f;

            // Stagger
            config.staggerChance = 0.4f;
            config.staggerDuration = 0.4f;

            // Spawning
            config.spawnWeight = 20;
            config.minRoomIteration = 3;

            // Type-specific
            config.jumpDistance = 10f;
            config.jumpCooldown = 3f;
            config.mediumRangeMin = 5f;
            config.mediumRangeMax = 12f;

            AssetDatabase.CreateAsset(config, path);
            Debug.Log($"Created Fast Enemy template at {path}");
        }
    }
}