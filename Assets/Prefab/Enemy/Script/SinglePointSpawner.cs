// SinglePointSpawner.cs
using UnityEngine;
using System.Collections.Generic;

public class SinglePointSpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject[] enemyPrefabs;
    [Tooltip("Базовый интервал спавна для этой точки (в секундах).")]
    public float baseSpawnInterval = 10f;
    private float currentEffectiveSpawnInterval;
    public float spawnRadius = 1f;

    [Header("Activation Settings (Optional)")]
    public bool requirePlayerNearby = false;
    public float activationRadius = 20f;
    public float deactivationRadius = 25f;
    private Transform playerTransform;
    private bool isActive = true;

    [Header("Limits (Optional)")]
    public int maxSpawnedFromThisPoint = 5;
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    private float nextSpawnTime;
    // private float lastAppliedModifier = -1f; // Можно убрать, если детальное логгирование модификатора не нужно постоянно

    public void ApplySpawnTimeModifier(float timeModifier)
    {
        float oldEffectiveInterval = currentEffectiveSpawnInterval;
        currentEffectiveSpawnInterval = Mathf.Max(0.1f, baseSpawnInterval * timeModifier);

        // Корректируем nextSpawnTime, если интервал изменился и следующий спавн еще не наступил
        if (Mathf.Abs(currentEffectiveSpawnInterval - oldEffectiveInterval) > 0.001f && Time.time < nextSpawnTime && oldEffectiveInterval > 0.001f)
        {
            float remainingTime = nextSpawnTime - Time.time;
            float ratio = currentEffectiveSpawnInterval / oldEffectiveInterval;
            nextSpawnTime = Time.time + remainingTime * ratio;
        }
        // Debug.Log($"Spawner '{gameObject.name}': ApplySpawnTimeModifier. Modifier: {timeModifier:F2}, Base: {baseSpawnInterval:F1}s, Effective: {currentEffectiveSpawnInterval:F2}s. NextSpawn in: {(nextSpawnTime - Time.time):F1}s");
    }

    void Start()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError($"SinglePointSpawner on {gameObject.name}: No enemy prefabs assigned! Disabling spawner.", this);
            enabled = false;
            return;
        }

        // Инициализируем currentEffectiveSpawnInterval.
        // GameManager вызовет ApplySpawnTimeModifier вскоре после этого, если его Start() выполнится раньше
        // или если это первый Update GameManager'а.
        if (GameManager.Instance != null)
        {
            currentEffectiveSpawnInterval = Mathf.Max(0.1f, baseSpawnInterval * GameManager.Instance.CurrentCalculatedSpawnTimeModifier);
        }
        else
        {
            currentEffectiveSpawnInterval = baseSpawnInterval; // Запасной вариант
        }
        // Debug.Log($"Spawner '{gameObject.name}' Start: Initial EffectiveInterval set to {currentEffectiveSpawnInterval:F2}s");

        if (requirePlayerNearby)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) { playerTransform = playerObj.transform; UpdateActivationState(); }
            else { Debug.LogWarning($"Spawner '{gameObject.name}': Player not found, 'Require Player Nearby' inactive.", this); isActive = true; }
        }
        else { isActive = true; }

        nextSpawnTime = Time.time + Random.Range(0.1f * currentEffectiveSpawnInterval, currentEffectiveSpawnInterval * 0.75f);
        // Debug.Log($"Spawner '{gameObject.name}' Start: First nextSpawnTime scheduled for {nextSpawnTime:F2} (in ~{(nextSpawnTime - Time.time):F2}s)");
    }

    void Update()
    {
        spawnedEnemies.RemoveAll(item => item == null);

        if (requirePlayerNearby && playerTransform != null)
        {
            UpdateActivationState();
        }

        if (!isActive)
        {
            return;
        }

        if (Time.time >= nextSpawnTime && spawnedEnemies.Count < maxSpawnedFromThisPoint)
        {
            // Debug.Log($"Spawner '{gameObject.name}': SPAWNING! Time: {Time.time:F2}. EffectiveInterval for next: {currentEffectiveSpawnInterval:F2}s.");
            SpawnEnemy();
            nextSpawnTime = Time.time + currentEffectiveSpawnInterval;
            // Debug.Log($"Spawner '{gameObject.name}': New nextSpawnTime scheduled for {nextSpawnTime:F2} (in {currentEffectiveSpawnInterval:F2}s)");
        }
    }

    void UpdateActivationState()
    {
        if (playerTransform == null) return;
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        bool previousActiveState = isActive; // Для отладки, если нужно

        if (isActive)
        {
            if (distanceToPlayer > deactivationRadius) isActive = false;
        }
        else // Был неактивен
        {
            if (distanceToPlayer <= activationRadius)
            {
                isActive = true;
                // При активации можно немного ускорить следующий спавн
                nextSpawnTime = Time.time + Random.Range(0.1f * currentEffectiveSpawnInterval, currentEffectiveSpawnInterval * 0.5f);
            }
        }
        // if(previousActiveState != isActive) Debug.Log($"Spawner '{gameObject.name}': Activation state changed to: {isActive}");
    }

    void SpawnEnemy()
    {
        if (enemyPrefabs.Length == 0) return;
        GameObject enemyToSpawn = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);
        GameObject newEnemy = Instantiate(enemyToSpawn, spawnPosition, Quaternion.identity);
        spawnedEnemies.Add(newEnemy); // Убедимся, что это здесь
    }

    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && isActive) // Рисуем только если спаунер активен в рантайме для наглядности
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 0.3f); // Маленькая сфера для индикации активности
        }
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        if (requirePlayerNearby)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, activationRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, deactivationRadius);
        }
    }
}