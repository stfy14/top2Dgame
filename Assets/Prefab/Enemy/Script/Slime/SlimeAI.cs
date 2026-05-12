// SlimeAI.cs
using UnityEngine;
using System.Collections; // ��� ������� (IEnumerator)

public class SlimeAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float jumpForce = 5f;
    public float jumpInterval; // ������� ������ �������
    public float jumpAngleVariance = 15f; // ��������� ������������� ���� ������
    public float horizontalForceMultiplier = 1f; // ������� ������� � ������� ������

    [Header("Targeting")]
    public string playerTag = "Player";
    private Transform playerTransform;

    [Header("Damage")]
    public int damage = 10;
    public float minMultiplyDamage = 0.3f;

    [Header("Ground Check (2D)")]
    public Transform groundCheckPoint; // ����� ��� ��������, �������� �� ����� �����
    public Vector2 groundCheckRadius;
    public LayerMask groundLayer; // ����, ������� ��������� ������

    private Rigidbody2D rb;
    private bool isGrounded;
    private float nextJumpTime = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("SlimeAI_2D: Rigidbody2D not found on " + gameObject.name);
            enabled = false; // ��������� ������, ���� ��� Rigidbody2D
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            Debug.LogWarning("SlimeAI_2D: Player with tag '" + playerTag + "' not found. Slime will jump randomly upwards.");
        }

        // �������� ������� ����� (��� � ��������� ���������)
        nextJumpTime = Time.time + Random.Range(0f, jumpInterval * 0.5f); // ��������� ������ ��� ������
    }

    void Update()
    {
        // �������� �� ����� (����� � � FixedUpdate, �� ��� Update ���� ������ ��� �����)
        CheckIfGrounded();

        if (Time.time >= nextJumpTime && isGrounded)
        {
            JumpTowardsPlayer();
            nextJumpTime = Time.time + jumpInterval;
        }
    }

    void CheckIfGrounded()
    {
        if (groundCheckPoint == null)
        {
            // ���� ����� �� ���������, �������, ��� ����� ������ �� ����� (���������)
            // ��� ��������� ��� Rigidbody.velocity.y
            isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.05f; // �������, �� �� ������ �������� ������
            return;
        }
        isGrounded = Physics2D.OverlapBox(groundCheckPoint.position, groundCheckRadius, groundLayer);
    }

    void JumpTowardsPlayer()
    {
        if (playerTransform == null)
        {
            // ���� ����� �� ������, ������� ������ �����
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            return;
        }

        // ���������� ����������� � ������
        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;

        // ������� ������ ������
        // �� ����� ������ � ��������� ����� �����, � �������������� ������������ ����� ���������� � ������
        Vector2 jumpDirection = Vector2.up; // �������� ����������� - �����
        jumpDirection.x = directionToPlayer.x * horizontalForceMultiplier; // ��������� �������������� ���� � ������

        // ��������� ������� ����������� � ���� ������, ����� �� ���� ������� ������������
        float randomAngleOffset = Random.Range(-jumpAngleVariance, jumpAngleVariance);
        jumpDirection = Quaternion.Euler(0, 0, randomAngleOffset) * jumpDirection;

        jumpDirection.Normalize(); // �����������, ����� ����� ���� ���� �������������

        rb.AddForce(jumpDirection * jumpForce, ForceMode2D.Impulse);
    }

    // ��������� ������������ ��� ��������� �����
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(playerTag))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                float _damageMultiply = Random.Range(minMultiplyDamage, 1f); 
                float damageAmount = damage * _damageMultiply;
                playerHealth.TakeDamage((int)damageAmount);
                rb.AddForce(-rb.linearVelocity.normalized * 6f, ForceMode2D.Impulse); 
            }
        }
    }

    // ��� ������������ GroundCheck � ���������
    void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheckPoint.position, groundCheckRadius);
        }
    }
}