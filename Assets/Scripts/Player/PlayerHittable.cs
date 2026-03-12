using System;
using Photon.Pun;
using UnityEngine;

public class PlayerHittable : MonoBehaviourPunCallbacks, IHittable
{
    [SerializeField] private PlayerMovement playerMovement;

    /// <summary>Invoked when this player is hit (owner only). Args: damage, source position. Subscribe for state, animation, UI, sound.</summary>
    public Action<int, Vector3> OnHitReceived;

    private void Awake()
    {
        if (!playerMovement)
            playerMovement = GetComponent<PlayerMovement>();
    }

    public void TakeDamage(int damageAmount, Transform damageSourceTransform, float knockbackForce = 0f, float knockbackDuration = 0f)
    {
        photonView.RPC(nameof(ApplyDamageAndKnockback), RpcTarget.All, damageAmount, damageSourceTransform.position, knockbackForce, knockbackDuration);
    }

    [PunRPC]
    private void ApplyDamageAndKnockback(int damage, Vector3 sourcePosition, float knockbackForce, float knockbackDuration)
    {
        if (!photonView.IsMine) return;

        // Apply damage (log for now; plug in health component later)
        // TODO: health?.TakeDamage(damage);
        Debug.Log($"[PlayerHittable] Took {damage} damage from {sourcePosition}");

        if (playerMovement && knockbackForce > 0f && knockbackDuration > 0f)
        {
            Vector2 direction = ((Vector2)transform.position - (Vector2)sourcePosition).normalized;
            if (direction.sqrMagnitude < 0.01f)
                direction = Vector2.right; // fallback if source and victim overlap
            playerMovement.SetKnockback(direction, knockbackForce, knockbackDuration);
        }

        OnHitReceived?.Invoke(damage, sourcePosition);
    }
}
