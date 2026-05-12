using UnityEngine;

public class ParallaxLoop : MonoBehaviour
{
    public float parallaxFactor = 0.5f;
    public bool loopX = true;

    private Transform cam;
    private Vector3 previousCamPos;
    private float spriteWidth;

    void Start()
    {
        cam = Camera.main.transform;
        previousCamPos = cam.position;

        // ѕолучаем ширину спрайта в мировых координатах
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        spriteWidth = sr.bounds.size.x;
    }

    void Update()
    {
        Vector3 deltaMovement = cam.position - previousCamPos;
        transform.position += new Vector3(deltaMovement.x * parallaxFactor, deltaMovement.y * parallaxFactor, 0);
        previousCamPos = cam.position;
    }
}
