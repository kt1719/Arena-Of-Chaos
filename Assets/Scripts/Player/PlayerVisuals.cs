using System;
using Photon.Pun;
using UnityEngine;

public class PlayerVisuals : MonoBehaviourPunCallbacks
{
    private readonly int ANIMATOR_MOVE_STRING_HASH = Animator.StringToHash("Moving");
    [SerializeField] private PlayerController playerController;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerHittable playerHittable;

    private void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerHittable = GetComponentInParent<PlayerHittable>();
        if (playerHittable != null)
            playerHittable.OnHitReceived += OnHitReceived;
    }

    private void OnDestroy()
    {
        if (playerHittable != null)
            playerHittable.OnHitReceived -= OnHitReceived;
    }

    private void OnHitReceived(int damage, Vector3 sourcePosition)
    {
        // Optional: trigger a "Hit" animator parameter if you add one, e.g. animator.SetTrigger("Hit");
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        UpdateAnimator();
        UpdatePlayerFacingDirection();
    }

    private void UpdatePlayerFacingDirection()
    {
        bool facingLeft = playerController.FacingLeft;

        if (photonView.IsMine && spriteRenderer.flipX != facingLeft)
        {
            photonView.RPC(nameof(RPC_SetFacingDirection), RpcTarget.All, facingLeft);
        }
    }

    [PunRPC]
    private void RPC_SetFacingDirection(bool facingLeft)
    {
        if (spriteRenderer == null) return;
        
        spriteRenderer.flipX = facingLeft;
    }

    private void UpdateAnimator()
    {
        PlayerState state = playerController.PlayerState;
        switch (state)
        {
            case PlayerState.Idle:
                animator.SetBool(ANIMATOR_MOVE_STRING_HASH, false);
                break;
            case PlayerState.Move:
                animator.SetBool(ANIMATOR_MOVE_STRING_HASH, true);
                break;
            case PlayerState.Knockback:
                animator.SetBool(ANIMATOR_MOVE_STRING_HASH, false);
                break;
        }
    }
}
