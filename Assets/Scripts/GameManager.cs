// GameManager.cs
using UnityEngine;
using System;
using UnityEngine.Events;
using UnityEngine.SceneManagement; // Для перезапуска сцены

public enum GameState { Playing, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameState currentGameState { get; private set; }

    [Header("Game Statistics")]
    private int enemiesKilledCount = 0;
    private float sessionStartTime;
    public float CurrentSessionTime { get; private set; }

    [Header("Dynamic Spawn Modifier Settings")]
    [Tooltip("Время в секундах, за которое спавн достигнет максимального ускорения.")]
    public float timeToMaxEffect = 180f;
    [Tooltip("Во сколько раз максимум может ускориться спавн (например, 4 = в 4 раза быстрее). Значение должно быть >= 1.")]
    public float maxSpawnSpeedMultiplierFactor = 4f;
    [Tooltip("Текущий модификатор времени спавна, применяемый к спаунерам (1.0 = норма, <1.0 = быстрее). Только для чтения.")]
    public float CurrentCalculatedSpawnTimeModifier { get; private set; }

    private float targetMinSpawnTimeModifier; // Целевой минимальный модификатор времени (1/maxSpawnSpeedMultiplierFactor)

    // --- UnityEvents ---
    [Header("Gameplay Events")]
    public UnityEvent<int, int> OnPlayerHealthUpdate;
    public UnityEvent<int> OnEnemiesKilledUpdate;
    public UnityEvent<float> OnSessionTimeUpdate;
    public UnityEvent<int, int> OnAmmoUpdate;
    public UnityEvent OnReloadStart;
    public UnityEvent<float> OnReloadProgress;
    public UnityEvent OnReloadComplete;

    [Header("Game State Events")]
    public UnityEvent OnGameOver;
    public UnityEvent OnGameRestart;

    [Header("Spawn Modifier Events")]
    [Tooltip("Событие для UI, передает десятичный модификатор (1.0 -> 0.25).")]
    public UnityEvent<float> OnSpawnTimeModifierUpdate; // Передает CurrentCalculatedSpawnTimeModifier
    [Tooltip("Событие для UI, передает фактор ускорения (1.0x -> 4.0x).")]
    public UnityEvent<float> OnSpawnSpeedFactorUpdate;  // Передает 1.0f / CurrentCalculatedSpawnTimeModifier


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Убрано для простого перезапуска сцены
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeNewGame();
    }

    void InitializeNewGame()
    {
        Time.timeScale = 1f;
        currentGameState = GameState.Playing;

        sessionStartTime = Time.time;
        CurrentSessionTime = 0f;
        enemiesKilledCount = 0;

        // Рассчитываем targetMinSpawnTimeModifier здесь, он зависит от настроек, которые не должны меняться во время сессии.
        targetMinSpawnTimeModifier = 1f / Mathf.Max(1f, maxSpawnSpeedMultiplierFactor);
        CurrentCalculatedSpawnTimeModifier = 1.0f; // Начальный действующий модификатор всегда 1.0

        Debug.Log($"InitializeNewGame: MaxFactor={maxSpawnSpeedMultiplierFactor}, TimeToMax={timeToMaxEffect}s. " +
                  $"TargetMinSpawnTimeModifier: {targetMinSpawnTimeModifier:F4}, Initial CurrentCalculatedSTM: {CurrentCalculatedSpawnTimeModifier:F4}");

        OnEnemiesKilledUpdate?.Invoke(enemiesKilledCount);
        OnSessionTimeUpdate?.Invoke(CurrentSessionTime);

        OnSpawnTimeModifierUpdate?.Invoke(CurrentCalculatedSpawnTimeModifier);
        if (CurrentCalculatedSpawnTimeModifier > 0.0001f)
            OnSpawnSpeedFactorUpdate?.Invoke(1.0f / CurrentCalculatedSpawnTimeModifier);

        ApplySpawnModifierToSpawners();
        OnGameRestart?.Invoke();
    }

    void Update()
    {
        if (currentGameState == GameState.Playing)
        {
            CurrentSessionTime = Time.time - sessionStartTime;
            OnSessionTimeUpdate?.Invoke(CurrentSessionTime);
            CalculateDynamicSpawnModifier();
        }
    }

    public void PlayerDied()
    {
        if (currentGameState == GameState.Playing)
        {
            currentGameState = GameState.GameOver;
            Time.timeScale = 0f;
            Debug.Log("Game Over! Time.timeScale set to 0.");
            OnGameOver?.Invoke();
        }
    }

    public void RestartGame()
    {
        Debug.Log("RestartGame called. Loading current scene...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void IncrementKills()
    {
        if (currentGameState != GameState.Playing) return;
        enemiesKilledCount++;
        OnEnemiesKilledUpdate?.Invoke(enemiesKilledCount);
    }

    private void CalculateDynamicSpawnModifier()
    {
        float previousAppliedModifier = CurrentCalculatedSpawnTimeModifier;

        // targetMinSpawnTimeModifier уже вычислен и не меняется в течение сессии
        // если maxSpawnSpeedMultiplierFactor не меняется.

        float effectProgress = Mathf.Clamp01(CurrentSessionTime / timeToMaxEffect);

        // Прямая интерполяция от начального модификатора (1.0) к целевому минимальному модификатору
        CurrentCalculatedSpawnTimeModifier = Mathf.Lerp(1.0f, targetMinSpawnTimeModifier, effectProgress);

        // Clamp здесь не нужен, если targetMinSpawnTimeModifier < 1.0f и effectProgress [0,1], 
        // Lerp(1.0f, target, progress) всегда будет в диапазоне [target, 1.0f].
        // Но для безопасности от некорректных настроек можно оставить:
        // CurrentCalculatedSpawnTimeModifier = Mathf.Clamp(CurrentCalculatedSpawnTimeModifier, targetMinSpawnTimeModifier, 1.0f);


        // Выводим отладочную информацию КАЖДЫЙ КАДР, чтобы видеть динамику
        // (можно закомментировать после отладки)
        // Debug.Log($"CALC_MOD: Time={CurrentSessionTime:F2}s, Progress={effectProgress:F3}, TargetMinSTM={targetMinSpawnTimeModifier:F4}, CurrentCalcSTM={CurrentCalculatedSpawnTimeModifier:F4}");


        if (Mathf.Abs(CurrentCalculatedSpawnTimeModifier - previousAppliedModifier) > 0.0001f) // Уменьшил порог для большей чувствительности
        {
            // Debug.Log($"GM Update: Modifier changed. Applying to spawners. NewSTM: {CurrentCalculatedSpawnTimeModifier:F4}");
            ApplySpawnModifierToSpawners();

            OnSpawnTimeModifierUpdate?.Invoke(CurrentCalculatedSpawnTimeModifier);

            if (CurrentCalculatedSpawnTimeModifier > 0.0001f)
            {
                float speedFactor = 1.0f / CurrentCalculatedSpawnTimeModifier;
                OnSpawnSpeedFactorUpdate?.Invoke(speedFactor);
                // Лог, который вы приводили, теперь будет здесь, и его частота зависит от этого if
                Debug.Log($"GameManager: Фактор ускорения (для UI): x{speedFactor:F1} (Десятичный: {CurrentCalculatedSpawnTimeModifier:F4})");
            }
        }
    }

    private void ApplySpawnModifierToSpawners()
    {
        SinglePointSpawner[] spawners = FindObjectsOfType<SinglePointSpawner>();
        foreach (var spawner in spawners)
        {
            spawner.ApplySpawnTimeModifier(CurrentCalculatedSpawnTimeModifier);
        }
    }

    // --- Trigger-методы для UI ---
    public void TriggerPlayerHealthUpdate(int current, int max) { OnPlayerHealthUpdate?.Invoke(current, max); }
    public void TriggerAmmoUpdate(int current, int magazineSize) { OnAmmoUpdate?.Invoke(current, magazineSize); }
    public void TriggerReloadStart() { OnReloadStart?.Invoke(); }
    public void TriggerReloadProgress(float progress) { OnReloadProgress?.Invoke(progress); }
    public void TriggerReloadComplete() { OnReloadComplete?.Invoke(); }
}