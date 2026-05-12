// SlimeParticle.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SlimeParticle : MonoBehaviour
{
    public float minLifetime = 0.5f;
    public float maxLifetime = 1.5f;
    public float initialForceMin = 1f;
    public float initialForceMax = 3f;
    // public Color particleColor = Color.green; // Если хотим задавать цвет через скрипт, а не на префабе

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Настройки Rigidbody лучше делать на префабе, но можно и здесь для гарантии
        rb.gravityScale = 1f; // Частицы будут падать
    }

    public void Initialize(Vector2 direction)
    {
        // Задаем случайную силу в указанном направлении
        float force = Random.Range(initialForceMin, initialForceMax);
        rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);

        // Задаем случайный крутящий момент для вращения (опционально)
        float torque = Random.Range(-10f, 10f);
        rb.AddTorque(torque);

        // Уничтожаем частицу через случайное время
        float lifetime = Random.Range(minLifetime, maxLifetime);
        Destroy(gameObject, lifetime);

        // Если цвет задается через скрипт:
        // SpriteRenderer sr = GetComponent<SpriteRenderer>();
        // if (sr != null) sr.color = particleColor;
    }
}