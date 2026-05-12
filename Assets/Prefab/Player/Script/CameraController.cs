using UnityEngine;

public class CameraController2D : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Объект, за которым будет следовать камера (обычно игрок).")]
    public Transform target;

    [Header("Following Settings")]
    [Tooltip("Насколько плавно камера следует за целью. Меньше значение = более резкое движение.")]
    [Range(0.01f, 1.0f)]
    public float smoothSpeed = 0.125f;
    [Tooltip("Смещение камеры относительно цели по X и Y.")]
    public Vector2 followOffset = Vector2.zero;

    [Header("Cursor Influence Settings")]
    [Tooltip("Включить ли влияние курсора на позицию камеры.")]
    public bool enableCursorInfluence = true;
    [Tooltip("Максимальное расстояние, на которое камера сместится в сторону курсора от позиции цели.")]
    public float cursorInfluenceStrength = 2f;
    [Tooltip("Радиус 'мертвой зоны' вокруг цели (в экранных пикселях). Если курсор внутри этой зоны, смещения не будет.")]
    public float cursorDeadZoneRadius = 50f;
    [Tooltip("Плавность возврата камеры из положения под влиянием курсора к положению за целью. Меньше значение = резче.")]
    [Range(0.01f, 1.0f)]
    public float cursorInfluenceSmoothSpeed = 0.05f;


    private Camera mainCamera;
    private Vector3 currentVelocity = Vector3.zero; 
    private float initialZ;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Target для CameraController2D не назначен!");
            enabled = false; // Отключаем скрипт, если нет цели
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Не найдена основная камера (MainCamera) в сцене!");
            enabled = false;
            return;
        }

        // Сохраняем начальную Z-позицию камеры. Для 2D она обычно не меняется.
        initialZ = transform.position.z;
    }

    // LateUpdate вызывается после всех Update, что хорошо для камер и процедурной анимации
    void FixedUpdate()
    {
        if (target == null || mainCamera == null) return;

        // 1. Базовая позиция следования за целью с учетом смещения
        Vector3 targetFollowPosition = new Vector3(
            target.position.x + followOffset.x,
            target.position.y + followOffset.y,
            initialZ // Используем сохраненную Z
        );

        // 2. Рассчитываем влияние курсора
        Vector3 cursorOffset = Vector3.zero;
        if (enableCursorInfluence)
        {
            // Позиция цели на экране
            Vector2 targetScreenPosition = mainCamera.WorldToScreenPoint(target.position);

            // Позиция курсора на экране
            Vector2 mouseScreenPosition = Input.mousePosition;

            // Вектор от цели к курсору на экране
            Vector2 directionToMouseScreen = mouseScreenPosition - targetScreenPosition;

            if (directionToMouseScreen.magnitude > cursorDeadZoneRadius)
            {
                // Нормализуем вектор и умножаем на силу влияния
                // Преобразуем экранный вектор в мировой, учитывая, что нам нужны только X и Y
                // Простой способ - взять направление и умножить на силу.
                // Более точный способ требует преобразования экранного смещения в мировое.
                // Для простоты здесь используем нормализованное направление в мировых координатах от камеры к курсору,
                // но смещение применяем относительно цели.

                // Мировая позиция курсора (на плоскости Z=0 относительно камеры)
                Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Mathf.Abs(mainCamera.transform.position.z - target.position.z)));
                // (mainCamera.transform.position.z - target.position.z) это расстояние от камеры до плоскости игрока
                // Mathf.Abs гарантирует, что значение положительное

                // Направление от ЦЕЛИ к МИРОВОЙ позиции курсора
                Vector3 worldDirectionToMouse = (mouseWorldPosition - target.position).normalized;
                worldDirectionToMouse.z = 0; // Игнорируем Z для 2D

                cursorOffset = worldDirectionToMouse * cursorInfluenceStrength;
            }
        }

        // 3. Конечная желаемая позиция камеры
        Vector3 desiredPosition = targetFollowPosition + cursorOffset;
        desiredPosition.z = initialZ; // Еще раз убедимся, что Z корректна

        // 4. Плавное перемещение камеры
        // Можно использовать Lerp или SmoothDamp. SmoothDamp обычно дает более "физическое" ощущение.
        // transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed); // Вариант с Lerp

        // Выбираем скорость сглаживания в зависимости от того, активно ли влияние курсора
        float currentSmoothSpeed = (cursorOffset != Vector3.zero && enableCursorInfluence) ? cursorInfluenceSmoothSpeed : smoothSpeed;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, currentSmoothSpeed);
    }

    // Опционально: отрисовка мертвой зоны в редакторе для наглядности
    void OnDrawGizmosSelected()
    {
        if (target != null && mainCamera != null && enableCursorInfluence)
        {
            // Gizmos рисуются в мировых координатах. Нам нужно преобразовать экранный радиус в мировой.
            // Это приблизительный способ, т.к. размер пикселя в мире зависит от ортографического размера или FOV/расстояния.
            // Для ортографической камеры:
            float worldRadius = 0;
            if (mainCamera.orthographic)
            {
                worldRadius = (cursorDeadZoneRadius / (float)mainCamera.pixelHeight) * 2f * mainCamera.orthographicSize;
            }
            else // Для перспективной (приблизительно)
            {
                float distance = Mathf.Abs(mainCamera.transform.position.z - target.position.z);
                float frustumHeight = 2.0f * distance * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                worldRadius = (cursorDeadZoneRadius / (float)mainCamera.pixelHeight) * frustumHeight;
            }

            Gizmos.color = new Color(0, 1, 0, 0.3f); // Зеленый, полупрозрачный
            Gizmos.DrawWireSphere(target.position, worldRadius); // Рисуем сферу вокруг цели
        }
    }
}