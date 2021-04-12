
using static LogUtility;
using UnityEngine;

public class PongBall : MonoBehaviour
{
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private float speedUpAmount = 0.5f;

    [HideInInspector] public bool allowFixedUpdate = false;

    private int collisionCount = 0;
    private Vector2 direction = Vector2.one.normalized;
    public Vector2 Direction
    {
        get { return direction; }
        set { direction = value.normalized; }
    }
    private Rigidbody2D rigid2D = null;

    private void Start()
    {
        if (rigid2D == null)
        {
            rigid2D = GetComponent<Rigidbody2D>();
            if (rigid2D == null) LogError("Pong ball is missing a 2D rigidbody!");
        }

        collisionCount = 0;
    }

    public Vector2 Position
    {
        get => rigid2D.position;
        set => rigid2D.MovePosition(value);
    }

    public void ResetBall()
    {
        rigid2D.transform.position = Vector2.zero;
        collisionCount = 0;
        Direction = Vector2.one.normalized;
    }

    private void FixedUpdate()
    {
        if (!allowFixedUpdate) return;
        rigid2D.MovePosition(rigid2D.position + direction * (speed + speedUpAmount * collisionCount) * Time.fixedDeltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!allowFixedUpdate) return;

        // Check if other collider is static or has a static rigidbody.
        // Because we know static objects won't move, we can predict physics on clients.
        if (!collision.gameObject.isStatic)
        {
            if (collision.rigidbody == null)
                return;

            if (collision.rigidbody.bodyType != RigidbodyType2D.Static)
                return;
        }

        Reflect(collision.GetContact(0).normal);

        //Debug.DrawLine(collision.GetContact(0).point, collision.GetContact(0).point + collision.GetContact(0).normal, Color.yellow, 1.0f);
        //Debug.DrawLine(collision.GetContact(0).point, collision.GetContact(0).point + direction, Color.red, 1.0f);
    }

    public void Reflect(Vector2 normal)
    {
        direction = Vector2.Reflect(direction, normal.normalized);
        collisionCount++;
    }
}
