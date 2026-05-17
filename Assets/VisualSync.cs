using UnityEngine;

public class VisualSync : MonoBehaviour
{
    [SerializeField] private BoxCollider2D parentCollider;

    // Para modificar al padre carajo!

    public void SetColliderSize(Vector2 size)
    {
        if (parentCollider != null)
            parentCollider.size = size;
    }
}