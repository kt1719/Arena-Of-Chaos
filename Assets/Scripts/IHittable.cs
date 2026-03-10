using UnityEngine;

public interface IHittable
{
    public void TakeDamage(int damageAmount, Transform damageSourceTransform);
}