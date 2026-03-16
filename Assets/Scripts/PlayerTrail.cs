using System;
using UnityEngine;

public class PlayerTrail : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    private TrailRenderer trailRenderer;

    private void Awake()
    {
        trailRenderer = GetComponent<TrailRenderer>();

        trailRenderer.emitting = false;
    }

    private void Start()
    {
        playerMovement.OnDashStart += PlayerController_OnDashStart;
        playerMovement.OnDashEnd += PlayerController_OnDashEnd;
    }

    private void PlayerController_OnDashEnd()
    {
        trailRenderer.emitting = false;
    }

    private void PlayerController_OnDashStart()
    {
        trailRenderer.emitting = true;
    }
}
