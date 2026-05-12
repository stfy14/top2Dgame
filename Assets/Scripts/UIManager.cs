// UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
// using UnityEngine.SceneManagement; // Не нужен здесь, т.к. RestartGame в GameManager

public class UIManager : MonoBehaviour
{
    [Header("In-Game UI Panels")]
    public GameObject gameplayUIParent;

    [Header("Health UI")]
    public Image healthBarImage;
    public TextMeshProUGUI healthText;

    [Header("Stats UI")]
    public TextMeshProUGUI sessionTimeText;
    public TextMeshProUGUI spawnModifierText;
    public TextMeshProUGUI killsCountText;

    [Header("Ammo UI")]
    public TextMeshProUGUI ammoCountText;
    public GameObject reloadIndicator; // Текст или иконка "RELOADING"
    public Image reloadProgressBar;    // Опционально: прогресс-бар для перезарядки

    [Header("Game Over UI")]
    public GameObject gameOverScreenPanel;
    public Button restartButton;

    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("UIManager: GameManager.Instance is not found!", this);
            enabled = false;
            return;
        }

        // Подписки на события GameManager
        GameManager.Instance.OnPlayerHealthUpdate.AddListener(UpdateHealthUI);
        GameManager.Instance.OnEnemiesKilledUpdate.AddListener(UpdateKillsUI);
        GameManager.Instance.OnSessionTimeUpdate.AddListener(UpdateSessionTimeUI);
        GameManager.Instance.OnSpawnSpeedFactorUpdate.AddListener(UpdateSwpawnModifire);
        GameManager.Instance.OnAmmoUpdate.AddListener(UpdateAmmoUI);
        GameManager.Instance.OnReloadStart.AddListener(ShowReloadIndicator);
        GameManager.Instance.OnReloadProgress.AddListener(UpdateReloadProgress);
        GameManager.Instance.OnReloadComplete.AddListener(HideReloadIndicator); // Теперь будет работать
        GameManager.Instance.OnGameOver.AddListener(ShowGameOverScreen);
        GameManager.Instance.OnGameRestart.AddListener(HideGameOverScreen); // Теперь будет работать

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonPressed);
        }

        if (gameOverScreenPanel != null) gameOverScreenPanel.SetActive(false);
        SetGameplayUIVisibility(true);
        HideReloadIndicator(); // Скрываем индикатор перезарядки при старте
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerHealthUpdate.RemoveListener(UpdateHealthUI);
            GameManager.Instance.OnEnemiesKilledUpdate.RemoveListener(UpdateKillsUI);
            GameManager.Instance.OnSessionTimeUpdate.RemoveListener(UpdateSessionTimeUI);
            GameManager.Instance.OnSpawnSpeedFactorUpdate.AddListener(UpdateSwpawnModifire);
            GameManager.Instance.OnAmmoUpdate.RemoveListener(UpdateAmmoUI);
            GameManager.Instance.OnReloadStart.RemoveListener(ShowReloadIndicator);
            GameManager.Instance.OnReloadProgress.RemoveListener(UpdateReloadProgress);
            GameManager.Instance.OnReloadComplete.RemoveListener(HideReloadIndicator);
            GameManager.Instance.OnGameOver.RemoveListener(ShowGameOverScreen);
            GameManager.Instance.OnGameRestart.RemoveListener(HideGameOverScreen);
        }
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartButtonPressed);
        }
    }

    // --- Реализации методов обновления UI (заполните их вашей логикой, если она отличается) ---
    void UpdateHealthUI(int currentHealth, int maxHealth)
    {
        if (healthBarImage != null && maxHealth > 0)
        {
            healthBarImage.fillAmount = (float)currentHealth / maxHealth;
        }
        if (healthText != null)
        {
            if (maxHealth > 0)
                healthText.text = $"{((float)currentHealth / maxHealth) * 100f:F0}%";
            else
                healthText.text = "N/A";
        }
    }

    void UpdateKillsUI(int kills)
    {
        if (killsCountText != null)
        {
            killsCountText.text = $"KILLS {kills}";
        }
    }

    void UpdateSessionTimeUI(float timeInSeconds)
    {
        if (sessionTimeText != null)
        {
            TimeSpan time = TimeSpan.FromSeconds(timeInSeconds);
            sessionTimeText.text = $"TIME {time:mm\\:ss}";
        }
    }

    void UpdateSwpawnModifire(float spawnModifire)
    {
        if  (spawnModifierText != null)
        {
            spawnModifierText.text = $"SPAWN {spawnModifire.ToString("F1")}x";
        }
    }

    void UpdateAmmoUI(int currentAmmo, int magazineSize)
    {
        if (ammoCountText != null)
        {
            ammoCountText.text = $"{currentAmmo} / {magazineSize}";
        }
    }

    void ShowReloadIndicator()
    {
        if (reloadIndicator != null) reloadIndicator.SetActive(true);
        if (reloadProgressBar != null)
        {
            reloadProgressBar.gameObject.SetActive(true);
            reloadProgressBar.fillAmount = 0;
        }
        TextMeshProUGUI reloadText = reloadIndicator?.GetComponent<TextMeshProUGUI>();
        if (reloadText != null) reloadText.text = "RELOADING...";
    }

    void UpdateReloadProgress(float progress)
    {
        if (reloadProgressBar != null && reloadProgressBar.gameObject.activeSelf)
        {
            reloadProgressBar.fillAmount = progress;
        }
        TextMeshProUGUI reloadText = reloadIndicator?.GetComponent<TextMeshProUGUI>();
        if (reloadText != null && reloadIndicator != null && reloadIndicator.activeSelf)
        {
            reloadText.text = $"RELOADING {Mathf.RoundToInt(progress * 100)}%";
        }
    }

    // ДОБАВЛЕН МЕТОД HideReloadIndicator
    void HideReloadIndicator()
    {
        if (reloadIndicator != null)
        {
            reloadIndicator.SetActive(false);
        }
        if (reloadProgressBar != null)
        {
            reloadProgressBar.gameObject.SetActive(false);
        }
    }
    // --- Конец реализаций методов обновления UI ---


    void SetGameplayUIVisibility(bool isVisible)
    {
        if (gameplayUIParent != null)
        {
            gameplayUIParent.SetActive(isVisible);
        }
        else
        {
            if (healthBarImage != null) healthBarImage.gameObject.SetActive(isVisible);
            if (healthText != null) healthText.gameObject.SetActive(isVisible);
            if (ammoCountText != null) ammoCountText.gameObject.SetActive(isVisible);
            // Индикатор перезарядки управляется отдельно через Show/HideReloadIndicator
        }
    }

    void ShowGameOverScreen()
    {
        if (gameOverScreenPanel != null) gameOverScreenPanel.SetActive(true);
        SetGameplayUIVisibility(false);
        HideReloadIndicator(); // Убедимся, что индикатор перезарядки скрыт
    }

    // ДОБАВЛЕН МЕТОД HideGameOverScreen
    void HideGameOverScreen()
    {
        if (gameOverScreenPanel != null) gameOverScreenPanel.SetActive(false);
        SetGameplayUIVisibility(true);
        // При перезапуске игры, если индикатор перезарядки был активен, 
        // он должен быть скрыт через OnReloadComplete или при инициализации GunSystem.
        // Здесь его специально показывать не нужно.
    }

    void OnRestartButtonPressed()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
    }
}