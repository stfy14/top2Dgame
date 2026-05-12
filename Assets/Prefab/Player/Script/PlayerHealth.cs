// PlayerHealth.cs
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    public UnityEvent<int> OnHealthChanged;
    public UnityEvent OnDeath;

    [Header("Damage Feedback")]
    public SpriteRenderer[] playerSpriteRenderers;
    public Color damageColor = Color.red;
    public float damageFlashDuration = 0.15f; // Сделал чуть короче для урона
    public AudioClip damageSound;

    [Header("Heal Feedback")] // NEW: Секция для отклика на лечение
    public Color healColor = Color.green;
    public float healFlashDuration = 0.25f; // Можно сделать чуть дольше для лечения
    public AudioClip healSound; // NEW: Звук лечения (если есть)

    private AudioSource audioSource;
    private Color[] originalColors;
    private Coroutine feedbackFlashCoroutine; // Одна корутина для обоих эффектов

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerPlayerHealthUpdate(currentHealth, maxHealth);
        }

        if (playerSpriteRenderers != null && playerSpriteRenderers.Length > 0)
        {
            originalColors = new Color[playerSpriteRenderers.Length];
            for (int i = 0; i < playerSpriteRenderers.Length; i++)
            {
                if (playerSpriteRenderers[i] != null)
                {
                    originalColors[i] = playerSpriteRenderers[i].color;
                }
                else
                {
                    originalColors[i] = Color.white;
                }
            }
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        FlashFeedbackEffect(damageColor, damageFlashDuration); // Используем общий метод

        if (damageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSound);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerPlayerHealthUpdate(currentHealth, maxHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            OnHealthChanged?.Invoke(currentHealth);
        }
    }

    // ОБНОВЛЕННЫЙ метод Heal
    public void Heal(int healAmount)
    {
        if (currentHealth <= 0) return; // Не лечить мертвых
        if (currentHealth >= maxHealth) return; // Не лечить, если здоровье уже полное (и не проигрывать эффект)

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        FlashFeedbackEffect(healColor, healFlashDuration); // NEW: Запускаем эффект зеленой вспышки

        if (healSound != null && audioSource != null) // NEW: Воспроизводим звук лечения
        {
            audioSource.PlayOneShot(healSound);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerPlayerHealthUpdate(currentHealth, maxHealth);
        }
        OnHealthChanged?.Invoke(currentHealth);
    }

    // ОБЩИЙ метод для вспышки цветом
    void FlashFeedbackEffect(Color flashColor, float duration)
    {
        if (playerSpriteRenderers == null || playerSpriteRenderers.Length == 0) return;

        if (feedbackFlashCoroutine != null)
        {
            StopCoroutine(feedbackFlashCoroutine);
            // Немедленно восстанавливаем оригинальные цвета перед запуском новой вспышки,
            // чтобы избежать "залипания" цвета от предыдущей прерванной вспышки.
            RestoreOriginalColors();
        }
        feedbackFlashCoroutine = StartCoroutine(FeedbackFlashCoroutine(flashColor, duration));
    }

    IEnumerator FeedbackFlashCoroutine(Color flashColor, float duration)
    {
        // Устанавливаем цвет вспышки
        for (int i = 0; i < playerSpriteRenderers.Length; i++)
        {
            if (playerSpriteRenderers[i] != null)
                playerSpriteRenderers[i].color = flashColor;
        }

        yield return new WaitForSeconds(duration);

        RestoreOriginalColors();
        feedbackFlashCoroutine = null;
    }

    void RestoreOriginalColors() // Вспомогательный метод для восстановления цветов
    {
        if (originalColors == null || playerSpriteRenderers == null) return;
        for (int i = 0; i < playerSpriteRenderers.Length; i++)
        {
            if (playerSpriteRenderers[i] != null && i < originalColors.Length) // Добавил проверку i < originalColors.Length
            {
                playerSpriteRenderers[i].color = originalColors[i];
            }
        }
    }

    void Die()
    {
        OnHealthChanged?.Invoke(currentHealth);
        OnDeath?.Invoke();
        if (GameManager.Instance != null) // Убедимся, что GameManager оповещен перед потенциальной деактивацией
        {
            GameManager.Instance.PlayerDied();
        }
        // gameObject.SetActive(false);
    }
}