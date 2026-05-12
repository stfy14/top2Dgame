// EnemyHealth.cs
using UnityEngine;
using System.Collections.Generic; // Если используете список тегов для хилки

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 50;
    private int currentHealth;

    [Header("Loot")]
    public GameObject healthPickupPrefab;
    [Range(0f, 1f)]
    public float healthPickupDropChance = 0.5f;
    public float healthPickupSpawnForce = 2f;

    [Header("Damage Effects")]
    public GameObject slimeParticlePrefab; // Ссылка на префаб частицы слайма
    public int particlesOnHitMin = 2;    // Мин. частиц при попадании
    public int particlesOnHitMax = 4;    // Макс. частиц при попадании
    public int particlesOnDeathMin = 8;  // Мин. частиц при смерти
    public int particlesOnDeathMax = 15; // Макс. частиц при смерти
    public float particleSpawnRadius = 0.5f; // Радиус, в котором появляются частицы вокруг точки попадания/смерти

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damageAmount)
    {
        // Не обрабатывать урон, если уже мертв
        if (currentHealth <= 0) return;

        currentHealth -= damageAmount;

        // NEW: Создаем частицы при получении урона
        int particleCount = Random.Range(particlesOnHitMin, particlesOnHitMax + 1);
        SpawnSlimeParticles(particleCount, transform.position); // Используем позицию врага как центр

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.IncrementKills();
        }

        TryDropHealthPickup();

        // NEW: Создаем БОЛЬШЕ частиц при смерти
        int particleCount = Random.Range(particlesOnDeathMin, particlesOnDeathMax + 1);
        SpawnSlimeParticles(particleCount, transform.position);

        Destroy(gameObject);
    }

    void TryDropHealthPickup()
    {
        if (healthPickupPrefab != null)
        {
            if (Random.value <= healthPickupDropChance)
            {
                GameObject pickupInstance = Instantiate(healthPickupPrefab, transform.position, Quaternion.identity);
                HealthPickup pickupScript = pickupInstance.GetComponent<HealthPickup>();
                if (pickupScript != null && healthPickupSpawnForce > 0)
                {
                    Vector2 spawnDirection = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(0.5f, 1f)).normalized;
                    pickupScript.AddSpawnForce(spawnDirection, healthPickupSpawnForce);
                }
            }
        }
    }

    // NEW: Метод для спавна частиц
    void SpawnSlimeParticles(int count, Vector3 spawnCenter)
    {
        if (slimeParticlePrefab == null) return;

        for (int i = 0; i < count; i++)
        {
            // Небольшое случайное смещение от центра спавна
            Vector2 randomOffset = Random.insideUnitCircle * particleSpawnRadius;
            Vector3 spawnPosition = spawnCenter + new Vector3(randomOffset.x, randomOffset.y, 0);

            GameObject particleInstance = Instantiate(slimeParticlePrefab, spawnPosition, Quaternion.identity);
            SlimeParticle particleScript = particleInstance.GetComponent<SlimeParticle>();

            if (particleScript != null)
            {
                // Задаем случайное направление разлета частиц
                Vector2 direction = Random.insideUnitCircle.normalized;
                // Если хотим, чтобы они больше летели вверх:
                // Vector2 direction = new Vector2(Random.Range(-1f, 1f), Random.Range(0.5f, 1f)).normalized;
                particleScript.Initialize(direction);
            }
        }
    }
}