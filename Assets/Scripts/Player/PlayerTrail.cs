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
        playerMovement.OnDashStart += OnDashStart;
        playerMovement.OnDashEnd += OnDashEnd;
    }

    private void OnDashEnd()
    {
        trailRenderer.emitting = false;
    }

    private void OnDashStart()
    {
        trailRenderer.emitting = true;
    }
}
