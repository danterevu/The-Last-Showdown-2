using UnityEngine;
using System;


/// Va en cada GameObject de zona. Requiere un BoxCollider2D en modo Trigger
/// que defina los límites del área de juego.
///
/// SETUP en Unity:
///   - Cada zona es un GameObject con BoxCollider2D (Is Trigger = true)
///   - El tamaño del collider define el área jugable
///   - Este componente debe estar en el mismo GameObject

[RequireComponent(typeof(BoxCollider2D))]

public class SpaceZoneBoundary : MonoBehaviour
{
    // El manager se suscribe a este evento para saber qué nave salió
    public event Action<GameObject, int> OnShipExited; // nave, playerNumber

    private BoxCollider2D bounds;

    private void Awake()
    {
        bounds = GetComponent<BoxCollider2D>();
        bounds.isTrigger = true;
    }

    // OnTriggerExit2D se dispara cuando un objeto SALE del trigger
    // para que funcione, al menos uno de los dos objetos necesita un Rigidbody2D
    
    private void OnTriggerExit2D(Collider2D other)
    {
        int player = 0;

        if (other.CompareTag("Player1")) player = 1;
        else if (other.CompareTag("Player2")) player = 2;
        else return; // no es una nave, ignorar

        OnShipExited?.Invoke(other.gameObject, player);
    }

    /// Devuelve si una posición está dentro de los límites de esta zona.
    /// Útil para validar spawns y para debug.
    public bool Contains(Vector2 point)
    {
        return bounds.OverlapPoint(point);
    }

    
    /// Centro del área en coordenadas del mundo.
    
    public Vector2 Center => (Vector2)transform.position + bounds.offset;
}
