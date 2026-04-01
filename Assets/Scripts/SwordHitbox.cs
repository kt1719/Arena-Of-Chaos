using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    [Header("Semi-Circle Dimensions")]
    public float attackRange = 1.5f;
    public float attackAngle = 90f; // Half-angle: 90 = semi-circle, 60 = narrower arc

    [Header("Depth (for 3D overlap)")]
    public float attackDepth = 2f;

    [Header("Gizmo")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0f, 0f, 0.35f);
    [SerializeField] private Color gizmoWireColor = Color.red;

    /// <summary>
    /// Returns true if a target position is within the semi-circle arc.
    /// </summary>
    public bool IsInsideArc(Vector2 aimDirection, Vector3 origin, Vector3 targetPosition)
    {
        float angle = Vector2.Angle(aimDirection, targetPosition - origin);
        return angle <= attackAngle;
    }

    private Vector2 GetAimDirection()
    {
        if (GameInput.Instance == null) return Vector2.up;

        Transform playerTransform = transform.parent != null ? transform.parent : transform;
        return GameInput.Instance.GetWeaponAimDirection(playerTransform);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Vector2 aim = GetAimDirection();
        if (aim.sqrMagnitude < 0.01f) return;

        Transform playerTransform = transform.parent != null ? transform.parent : transform;
        Vector3 origin = playerTransform.position;

        float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;

        // Draw filled arc
        Gizmos.color = gizmoColor;
        int segments = 30;
        Vector3 prevPoint = origin;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float currentAngle = baseAngle - attackAngle + (t * attackAngle * 2f);
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 point = origin + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * attackRange;

            if (i > 0)
            {
                Gizmos.DrawLine(prevPoint, point);
                Gizmos.DrawLine(origin, point);
            }

            prevPoint = point;
        }

        // Draw wire arc outline
        Gizmos.color = gizmoWireColor;
        prevPoint = origin;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float currentAngle = baseAngle - attackAngle + (t * attackAngle * 2f);
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 point = origin + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * attackRange;

            if (i > 0)
                Gizmos.DrawLine(prevPoint, point);

            prevPoint = point;
        }

        // Close the arc edges
        float startRad = (baseAngle - attackAngle) * Mathf.Deg2Rad;
        float endRad = (baseAngle + attackAngle) * Mathf.Deg2Rad;
        Gizmos.DrawLine(origin, origin + new Vector3(Mathf.Cos(startRad), Mathf.Sin(startRad), 0f) * attackRange);
        Gizmos.DrawLine(origin, origin + new Vector3(Mathf.Cos(endRad), Mathf.Sin(endRad), 0f) * attackRange);

        // Aim line
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + (Vector3)(aim.normalized * attackRange));
    }
}