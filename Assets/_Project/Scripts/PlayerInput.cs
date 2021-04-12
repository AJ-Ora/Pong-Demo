
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public Vector2 Position
    {
        get => Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
}
