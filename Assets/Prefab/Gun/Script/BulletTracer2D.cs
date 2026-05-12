// BulletTracer2D.cs
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class BulletTracer2D : MonoBehaviour
{
    [Header("Bullet Parameters")]
    public float speed = 60f;
    public float maxLifetime = 1.0f;
    [Tooltip("Изменение переменной здесь не даст результата. Изменять урон нужно в самом оружии")]
    private int damage; // NEW: Урон, который наносит эта пуля

    [Header("Collision Settings")]
    [Tooltip("Список тегов объектов, при столкновении с которыми трассер будет уничтожен (например, 'Terrain', 'Wall').")]
    public List<string> destroyOnCollisionWithTags = new List<string>();
    // public LayerMask hitLayers; // NEW: Слои, с которыми пуля взаимодействует (опционально, если тегов недостаточно)

    private Rigidbody2D rb;
    private float lifetimeTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Ваша логика rb.bodyType и rb.gravityScale остается
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;
    }

    // ОБНОВЛЕННЫЙ Initialize
    public void Initialize(Vector2 direction, int bulletDamage/*, LayerMask layersForBullet*/) // Добавили урон
    {
        lifetimeTimer = 0f;
        damage = bulletDamage; // Устанавливаем урон
        // hitLayers = layersForBullet; // Если будете использовать LayerMask для пули

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Ваша логика движения пули остается
        if (rb != null) // Добавил проверку на rb на всякий случай
        {
            rb.linearVelocity = transform.right * speed;
        }
        else
        {
            Debug.LogError("Rigidbody2D is null on bullet, cannot set velocity!", this);
        }

        Destroy(gameObject, maxLifetime); // Используем maxLifetime из поля класса
    }

    // Старый Initialize, если он где-то вызывается, его нужно будет обновить или удалить.
    // Я его пока закомментирую, чтобы избежать путаницы.
    // public void Initialize(Vector2 direction) 
    // {
    //    Initialize(direction, 10); // Пример с уроном по умолчанию
    // }

    void Update()
    {
        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= maxLifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Игнорируем столкновения с игроком или его оружием
        if (other.CompareTag("Player") || other.CompareTag("Weapon")) // "Weapon" - предполагаемый тег для объекта оружия игрока
        {
            return;
        }

        // Ваша логика уничтожения по тегам остается
        if (destroyOnCollisionWithTags.Count > 0)
        {
            foreach (string tagToCompare in destroyOnCollisionWithTags)
            {
                if (!string.IsNullOrEmpty(tagToCompare) && other.CompareTag(tagToCompare))
                {
                    // Debug.Log($"Tracer hit specified tag '{tagToCompare}': {other.name}");
                    Destroy(gameObject);
                    return;
                }
            }
        }

        // NEW: Нанесение урона врагу
        if (other.CompareTag("Enemy")) // Ваша проверка на врага
        {
            // Debug.Log("Tracer hit Enemy: " + other.name);
            EnemyHealth enemy = other.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage); // Наносим урон
            }
            Destroy(gameObject); // Уничтожаем пулю после попадания во врага
            return;
        }
        // else
        // {
        //    Debug.Log("Different tag " + other.name);
        // }

        // Если пуля столкнулась с чем-то еще (не игрок, не оружие, не враг, не один из destroyOnCollisionWithTags),
        // но у этого объекта есть коллайдер, она все равно должна уничтожиться, чтобы не лететь вечно.
        // Это произойдет, если объект не имеет одного из "игнорируемых" или "специально обрабатываемых" тегов.
        // Этот блок можно убрать, если destroyOnCollisionWithTags покрывает все типы препятствий.
        // Destroy(gameObject); // Раскомментируйте, если хотите, чтобы пуля уничтожалась при столкновении с любым "неопознанным" коллайдером
    }
}