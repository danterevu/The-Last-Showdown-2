using UnityEngine;
using System.Collections.Generic;

public class TurretRangeDetector : MonoBehaviour
{
    private SpaceLaserTurret turret;

    private void Awake()
    {
        turret = GetComponentInParent<SpaceLaserTurret>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (turret != null && (other.CompareTag("Player1") || other.CompareTag("Player2")))
        {
            turret.AddTarget(other.transform);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (turret != null && (other.CompareTag("Player1") || other.CompareTag("Player2")))
        {
            turret.RemoveTarget(other.transform);
        }
    }
}
