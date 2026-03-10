using System;
using Photon.Pun;
using UnityEngine;

public class PlayerTrail : MonoBehaviourPunCallbacks
{
    [SerializeField] private PlayerMovement playerMovement;
    private TrailRenderer trailRenderer;

    private void Awake()
    {
        trailRenderer = GetComponent<TrailRenderer>();
        trailRenderer.emitting = false;
    }

    private void Update()
    {
        ChangeVisbilityIfPlayerDashing(playerMovement.IsDashing);
    }

    private void ChangeVisbilityIfPlayerDashing(bool isDashing)
    {
        trailRenderer.emitting = isDashing;
    }
}
