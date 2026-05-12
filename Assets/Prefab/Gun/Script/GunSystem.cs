// GunSystem.cs
using UnityEngine;
using System.Collections;

public class GunSystem : MonoBehaviour
{
    [Header("Assets")]
    public GameObject bulletTracerPrefab;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptyClipSound;

    [Header("Gun Stats")]
    public float fireRate = 4f;
    // public float weaponRange = 50f; // Теперь менее актуально для нанесения урона, но может использоваться для других целей
    public int weaponDamage = 10;
    public LayerMask shootableLayers; // МОЖЕТ ИСПОЛЬЗОВАТЬСЯ ПУЛЕЙ для определения, с чем ей сталкиваться

    [Header("Ammo & Reload")]
    public int magazineSize = 30;
    private int currentAmmoInMagazine;
    public float reloadTime = 1.5f;
    private bool isReloading = false;
    public KeyCode reloadKey = KeyCode.R;
    public bool autoReloadOnEmpty = true;

    [Header("References")]
    public Transform firePoint;
    private AudioSource audioSource;
    private float nextFireTimestamp = 0f;

    // --- Awake, Start, OnEnable, Update, TryShoot, StartReload, ReloadWeaponCoroutine ---
    // --- ОСТАЮТСЯ БЕЗ ИЗМЕНЕНИЙ, так как они управляют логикой боезапаса, перезарядки и темпа стрельбы ---
    // --- Я их не буду копировать сюда снова для краткости, используйте вашу последнюю рабочую версию этих методов ---
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (firePoint == null)
        {
            firePoint = transform.Find("FirePoint") ?? transform;
            if (firePoint == transform) Debug.LogWarning($"FirePoint не назначен на {gameObject.name} и не найден как дочерний. Используется transform объекта.", this);
        }

        if (bulletTracerPrefab == null)
        {
            Debug.LogError($"Bullet Tracer Prefab (префаб пули) не назначен на GunSystem ({gameObject.name})!", this);
            enabled = false;
            return;
        }
        currentAmmoInMagazine = magazineSize;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerAmmoUpdate(currentAmmoInMagazine, magazineSize);
        }
    }

    void OnEnable()
    {
        isReloading = false;
        StopAllCoroutines();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerAmmoUpdate(currentAmmoInMagazine, magazineSize);
            if (!isReloading) GameManager.Instance.TriggerReloadComplete();
        }
    }

    void Update()
    {
        if (isReloading)
        {
            return;
        }

        if (Input.GetKeyDown(reloadKey) && currentAmmoInMagazine < magazineSize)
        {
            StartReload();
            return;
        }

        if (autoReloadOnEmpty && currentAmmoInMagazine <= 0 && magazineSize > 0)
        {
            StartReload();
            return;
        }

        // Если GunSystem сам обрабатывает кнопку стрельбы:
        if (Input.GetButton("Fire1"))
        {
            TryShoot();
        }
    }

    public void TryShoot()
    {
        if (isReloading)
        {
            return;
        }

        if (currentAmmoInMagazine <= 0)
        {
            if (emptyClipSound != null && audioSource != null) audioSource.PlayOneShot(emptyClipSound);

            if (autoReloadOnEmpty && magazineSize > 0)
            {
                StartReload();
            }
            return;
        }

        if (Time.time >= nextFireTimestamp)
        {
            ProcessShoot(); // Вызываем обновленный ProcessShoot
            nextFireTimestamp = Time.time + 1f / fireRate;

            currentAmmoInMagazine--;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerAmmoUpdate(currentAmmoInMagazine, magazineSize);
            }

            if (autoReloadOnEmpty && currentAmmoInMagazine <= 0 && magazineSize > 0)
            {
                StartReload();
            }
        }
    }
    // --- Конец неизмененных методов ---

    private void ProcessShoot() // ОБНОВЛЕННЫЙ ProcessShoot
    {
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        Vector3 mousePositionWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePositionWorld.z = firePoint.position.z;
        Vector2 shootDirection = (mousePositionWorld - firePoint.position).normalized;

        if (bulletTracerPrefab != null)
        {
            GameObject bulletInstance = Instantiate(bulletTracerPrefab, firePoint.position, Quaternion.identity);

            BulletTracer2D bulletScript = bulletInstance.GetComponent<BulletTracer2D>();
            if (bulletScript != null)
            {
                // Передаем направление, урон ОРУЖИЯ, скорость из пули (или можно тоже из оружия), 
                // время жизни из пули (или из оружия), и слои для столкновения
                // Предполагаем, что у BulletTracer2D есть поля для скорости и времени жизни,
                // которые используются по умолчанию, если мы не передаем их здесь.
                // Если вы хотите, чтобы GunSystem полностью контролировал эти параметры,
                // добавьте их в Initialize пули и передавайте отсюда.
                bulletScript.Initialize(
                    shootDirection,
                    weaponDamage // Передаем урон из GunSystem
                                 // bulletScript.speed, // Используем скорость, заданную на префабе пули
                                 // bulletScript.maxLifetime, // Используем время жизни, заданное на префабе пули
                                 // shootableLayers // Передаем LayerMask, если пуля будет его использовать
                                 // (нужно раскомментировать параметр в Initialize пули)
                );
            }
            else
            {
                Debug.LogError($"Скрипт BulletTracer2D не найден на префабе пули '{bulletTracerPrefab.name}'!", bulletInstance);
            }
        }
        else
        {
            Debug.LogError("bulletTracerPrefab is null in ProcessShoot, cannot fire.");
            return; // Не должно произойти из-за проверки в Awake
        }

        // УДАЛЯЕМ ЛОГИКУ RAYCAST ОТСЮДА
        // RaycastHit2D hit = Physics2D.Raycast(firePoint.position, shootDirection, weaponRange, shootableLayers);
        // if (hit.collider != null)
        // {
        //    EnemyHealth enemy = hit.collider.GetComponent<EnemyHealth>();
        //    if (enemy != null)
        //    {
        //        enemy.TakeDamage(weaponDamage);
        //    }
        // }
    }

    // --- StartReload и ReloadWeaponCoroutine остаются без изменений ---
    private void StartReload()
    {
        if (isReloading) return;
        if (currentAmmoInMagazine == magazineSize && magazineSize > 0) return;

        isReloading = true;
        if (reloadSound != null && audioSource != null) audioSource.PlayOneShot(reloadSound);

        if (GameManager.Instance != null) GameManager.Instance.TriggerReloadStart();
        StartCoroutine(ReloadWeaponCoroutine());
    }

    private IEnumerator ReloadWeaponCoroutine()
    {
        float elapsedTime = 0f;
        while (elapsedTime < reloadTime)
        {
            elapsedTime += Time.deltaTime;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerReloadProgress(elapsedTime / reloadTime);
            }
            yield return null;
        }

        currentAmmoInMagazine = magazineSize;
        isReloading = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerAmmoUpdate(currentAmmoInMagazine, magazineSize);
            GameManager.Instance.TriggerReloadComplete();
        }
    }
    // --- Конец неизмененных методов ---
}