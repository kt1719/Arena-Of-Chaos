using Fusion;
using UnityEngine;

public interface IHittable
{
    void ApplyHit(int damage, Vector2 hitDirection, float knockbackForce, float knockbackDuration, PlayerRef attacker);
}
