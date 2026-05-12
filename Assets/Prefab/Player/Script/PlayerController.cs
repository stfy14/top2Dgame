// Твои using директивы
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] public float moveSpeed = 2f;
    [SerializeField] public float jumpForce = 20f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private GunSystem activeGun; // Ссылка на систему оружия

    public Transform groundCheck;
    public LayerMask groundLayer;

    [Header("Body Parts")]
    public Transform head;
    public Transform body;
    public Transform frontArm;
    public Transform backArm;
    public Transform weapon; // Этот Transform теперь должен иметь компонент GunSystem

    [Header("SpriteRenderer arm")]
    public SpriteRenderer SfrontArm;
    public SpriteRenderer SbackArm;

    [Header("Ground Check")]
    public Vector2 groundCheckSize;

    private bool isGrounded;
    [SerializeField] private bool isFacingRight = true;

    void Awake()
    {
        // Получаем GunSystem с объекта weapon
        if (weapon != null)
        {
            activeGun = weapon.GetComponent<GunSystem>();
            if (activeGun == null)
            {
                Debug.LogError($"Объект Weapon ({weapon.name}) не имеет компонента GunSystem!", weapon);
            }
        }
        else
        {
            Debug.LogError("Transform Weapon не назначен в PlayerController. Стрельба не будет работать.", this);
        }
    }

    void Update()
    {
        Jump();
        RotateBodyPartsToCursor(); // Твоя логика вращения остается здесь

        // Обработка стрельбы
        if (Input.GetButton("Fire1")) // "Fire1" - обычно левая кнопка мыши
        {
            if (activeGun != null)
            {
                activeGun.TryShoot();
            }
        }
    }

    void FixedUpdate()
    {
        Move();
    }

    // Твои методы Move(), Jump(), RotateBodyPartsToCursor(), OnDrawGizmosSelected()
    // ОСТАЮТСЯ БЕЗ ИЗМЕНЕНИЙ В ИХ ВНУТРЕННЕМ КОДЕ.
    // Я скопирую их сюда для полноты, но твоя логика в них сохраняется.

    void Move()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");

        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y); // Сохраняем Y скорость

            if (animator) animator.SetFloat("Speed", Mathf.Abs(moveInput));

            bool isMovingBackward = false;
            if (moveInput != 0)
            {
                if (isFacingRight && moveInput < 0)
                {
                    isMovingBackward = true;
                }
                else if (!isFacingRight && moveInput > 0)
                {
                    isMovingBackward = true;
                }
            }

            if (animator) animator.SetFloat("WalkDirectionMultiplier", isMovingBackward ? -1.0f : 1.0f);
        }
        else
        {
            // Если в воздухе, возможно, стоит управлять rb.velocity иначе или обнулять анимацию
            if (animator) animator.SetFloat("Speed", 0); // Пример: останавливаем анимацию ходьбы в воздухе
        }
    }

    void Jump()
    {
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer); // Добавил 0f для angle

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        if (animator) animator.SetBool("IsJumping", !isGrounded);
    }

    void RotateBodyPartsToCursor()
    {
        var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = transform.position.z; // Устанавливаем Z, чтобы избежать проблем с перспективой

        // ВАЖНО: Направление для вращения оружия и частей тела
        // Если weapon - это точка вращения для рук/головы, то используем weapon.position
        // Если части тела вращаются вокруг другой точки, измени это.
        // Твой код использует transform.position (т.е. центр игрока) для вычисления direction,
        // а затем weapon.transform.rotation для самого оружия.
        // Это может привести к тому, что оружие вращается вокруг своей оси, но эта ось движется вместе с transform.position
        // а остальные части тела вращаются вокруг transform.position
        // Для сохранения твоей логики:
        Vector3 directionToMouseForBodyParts = mousePos - transform.position; // Для головы и рук, если они вращаются вокруг центра игрока
        Quaternion bodyPartsRotation = Quaternion.LookRotation(Vector3.forward, directionToMouseForBodyParts) * Quaternion.Euler(0, 0, 90);

        // Для оружия, если оно вращается вокруг своей точки weapon.position:
        Vector3 directionToMouseForWeapon = mousePos - weapon.position;
        Quaternion weaponRotation = Quaternion.LookRotation(Vector3.forward, directionToMouseForWeapon) * Quaternion.Euler(0, 0, 90);


        // Применяем вращение согласно твоей логике
        if (weapon) weapon.transform.rotation = weaponRotation; // Оружие вращается вокруг своей точки
        if (head) head.transform.rotation = bodyPartsRotation;   // Голова вокруг центра игрока
        if (frontArm) frontArm.transform.rotation = bodyPartsRotation; // Рука вокруг центра игрока
        if (backArm) backArm.transform.rotation = bodyPartsRotation; // Рука вокруг центра игрока


        // Логика флипа, как у тебя была
        bool shouldFaceRightBasedOnMouse = mousePos.x > transform.position.x;

        if (isFacingRight != shouldFaceRightBasedOnMouse)
        {
            isFacingRight = shouldFaceRightBasedOnMouse;

            // Флип тела
            if (body != null)
            {
                // Твой код для body.transform.rotation:
                // body.transform.rotation = Quaternion.Euler(0, isFacingRight ? 0 : 180, 0);
                // Это лучше, чем scale, если части тела не должны наследоваться от флипа родителя.
                // Если body - это просто спрайт, то flipX у SpriteRenderer может быть лучше.
                // Если body - контейнер, то Euler(0, 180, 0) переворачивает его и всех детей.
                // Сохраняю твой подход:
                body.transform.rotation = Quaternion.Euler(0, isFacingRight ? 0 : 180, 0);
            }

            // Флип оружия по Y-scale
            if (weapon != null)
            {
                Vector3 weaponScale = weapon.transform.localScale;
                weaponScale.y = isFacingRight ? Mathf.Abs(weaponScale.y) : -Mathf.Abs(weaponScale.y); // Обеспечиваем, что знак меняется
                weapon.transform.localScale = weaponScale;
            }

            // Флип рук через SpriteRenderer.flipY
            if (SfrontArm != null) SfrontArm.flipY = !isFacingRight; // Если смотрит вправо, не флипаем, если влево - флипаем
            if (SbackArm != null) SbackArm.flipY = !isFacingRight;  // Аналогично

            // Флип головы по Y-scale (как у тебя было)
            if (head != null)
            {
                Vector3 headScale = head.transform.localScale;
                headScale.y = isFacingRight ? Mathf.Abs(headScale.y) : -Mathf.Abs(headScale.y); // Обеспечиваем, что знак меняется
                head.transform.localScale = headScale;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}