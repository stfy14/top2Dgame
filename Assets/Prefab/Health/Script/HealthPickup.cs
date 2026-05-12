// HealthPickup.cs
using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    public int healAmount = 25;
    public AudioClip pickupSound;
    // public GameObject pickupEffectPrefab;

    private Rigidbody2D rb; // ћожно добавить дл€ управлени€ физикой, если нужно
    private bool collected = false; // ‘лаг, чтобы избежать многократного подбора

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("HealthPickup: Rigidbody2D not found!", this);
            enabled = false;
        }
        // ”бедимс€, что Rigidbody2D настроен на Dynamic, если мы ожидаем физику
        // (это лучше делать на префабе, но можно и здесь дл€ подстраховки)
        if (rb.bodyType != RigidbodyType2D.Dynamic)
        {
            Debug.LogWarning("HealthPickup: Rigidbody2D on " + gameObject.name + " is not Dynamic. It might not fall correctly. Set it to Dynamic on the prefab.", this);
        }
    }

    // »спользуем OnCollisionEnter2D, так как Is Trigger сн€т
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collected) return; // ≈сли уже подобрана, ничего не делаем

        // ѕровер€ем, столкнулись ли мы с игроком
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            // Ћечим, если здоровье не полное (и если игрок вообще есть)
            if (playerHealth != null && playerHealth.currentHealth < playerHealth.maxHealth)
            {
                playerHealth.Heal(healAmount);
                collected = true; // ѕомечаем как подобранную

                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }

                // if (pickupEffectPrefab != null)
                // {
                //    Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
                // }

                Destroy(gameObject);
            }
            // ≈сли здоровье полное, хилка просто останетс€ лежать (или можно добавить логику отталкивани€)
        }
        // ≈сли хилка столкнулась с землей или стеной, она просто останетс€ лежать,
        // так как у нее теперь физический коллайдер и динамический Rigidbody.
    }

    // ќпционально: можно добавить небольшую силу при спавне, чтобы хилка "вылетала" из врага
    public void AddSpawnForce(Vector2 forceDirection, float forceMagnitude)
    {
        if (rb != null)
        {
            rb.AddForce(forceDirection.normalized * forceMagnitude, ForceMode2D.Impulse);
        }
    }
}