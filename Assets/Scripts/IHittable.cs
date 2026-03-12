using UnityEngine;

public interface IHittable
{
    public void TakeDamage(int damageAmount, Transform damageSourceTransform, float knockbackForce = 0f, float knockbackDuration = 0f);
}