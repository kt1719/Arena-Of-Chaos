using UnityEngine;

public class SlimeVisual : MonoBehaviour
{
    [SerializeField] private EnemyPathfinding enemyPathfinding;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Update()
    {
        if (enemyPathfinding.FacingLeft)
        {
            spriteRenderer.flipX = true;
        } else
        {
            spriteRenderer.flipX = false;
        }
    }
}
