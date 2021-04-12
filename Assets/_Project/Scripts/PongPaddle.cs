
using UnityEngine;

public class PongPaddle : MonoBehaviour
{
    [SerializeField] private Vector2 limits = new Vector2(-7.0f, 7.0f);

    public float GetPosition() => transform.position.y;
    public void SetPosition(float pos) => transform.position = new Vector3(transform.position.x, Mathf.Clamp(pos, limits.x, limits.y), transform.position.z);
}
