using System.Collections;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

public class PlayerKnockback : NetworkBehaviour
{
    // ===== Networked Fields =====
    [Networked] public Vector2 knockbackOffset { get; private set; }

    // ===== Serialized Fields =====
    [SerializeField] private NetworkRigidbody2D _networkRigidbody;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private Transform _playerVisual;
    [SerializeField] private float _snapTolerance = 0.1f;
    [SerializeField] private float _largeMoveTolerance = 0.1f; // Might need to make this more general and based on knockback magnitude value.
    [SerializeField] private float _predictionTimeout = 0.5f;

    // ===== Private Fields =====
    private bool _visualDetached;
    private Vector2 _predictedDestination;
    private Vector2 _originalPosition;
    private float _predictionStartTime;

    // ===== Server Authority =====
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (knockbackOffset != Vector2.zero)
        {
            Vector2 destination = (Vector2)transform.position + knockbackOffset;
            Debug.Log($"Server teleporting to {destination}");
            _networkRigidbody.Teleport(destination);
            knockbackOffset = Vector2.zero;
        }
    }

    // ===== Client Reconciliation =====
    public override void Render()
    {
        if (HasStateAuthority) return;

        if (_visualDetached && !HasInputAuthority && !HasStateAuthority) {
            Vector2 currentPos = transform.position;

            // Normal case — server teleport landed close to our prediction
            bool arrivedAtPrediction = Vector2.Distance(currentPos, _predictedDestination) < _snapTolerance;

            // Packet loss case — transform jumped far from original, but not where we predicted
            bool largeMoveFromOrigin = Vector2.Distance(currentPos, _originalPosition) > _largeMoveTolerance;

            // Safety net — something went wrong, don't leave visual detached forever
            bool timedOut = Time.time - _predictionStartTime > _predictionTimeout;

            if (arrivedAtPrediction)
            {
                RestoreVisual();
            }
            else if (largeMoveFromOrigin || timedOut)
            {
                // Snap visual to wherever the server actually put us
                _playerVisual.position = currentPos;
                RestoreVisual();
            }
        }
    }

    private void RestoreVisual()
    {
        _visualDetached = false;
        _playerVisual.parent = _playerTransform;
        StartCoroutine(SmoothVisualToLocal());

        Debug.Log($"Knockback reconciled — visual restored at {transform.position}");
    }

    private IEnumerator SmoothVisualToLocal()
    {
        float interpolationSpeed = 15f;
        while (Vector2.Distance(_playerVisual.localPosition, Vector2.zero) > 0.01f)
        {
            _playerVisual.localPosition = Vector3.Lerp(
                _playerVisual.localPosition,
                Vector3.zero,
                Time.deltaTime * interpolationSpeed
            );
            yield return null;
        }

        _networkRigidbody.InterpolationTarget = _playerVisual;
        _playerVisual.localPosition = Vector3.zero;
    }

    // ===== Knockback Entry Point =====
    public void ApplyKnockback(Vector2 hitDirection, float knockbackForce, float knockbackDuration)
    {
        Vector2 offset = GetKnockbackOffset(hitDirection, knockbackForce, knockbackDuration);

        if (!HasStateAuthority && !HasInputAuthority)
        {
            ApplyKnockbackPrediction(offset);
        }
        else
        {
            ApplyKnockbackPhysics(offset);
        }
    }

    // ===== Client Visual Prediction =====
    private void ApplyKnockbackPrediction(Vector2 offset)
    {
        if (_visualDetached) return;

        _originalPosition = transform.position;
        _predictedDestination = _originalPosition + offset;
        _predictionStartTime = Time.time;

        _visualDetached = true;
        _networkRigidbody.InterpolationTarget = null;
        _playerVisual.parent = null;
        _playerVisual.position = _predictedDestination;

        Debug.Log($"Client prediction — visual to {_predictedDestination}");
    }

    // ===== Server Physics =====
    private void ApplyKnockbackPhysics(Vector2 offset)
    {
        if (!HasStateAuthority) return;

        if (knockbackOffset != Vector2.zero) return;

        knockbackOffset = offset;
    }

    private Vector2 GetKnockbackOffset(Vector2 hitDirection, float knockbackForce, float knockbackDuration)
    {
        float knockbackDistance = knockbackForce * knockbackDuration;
        return hitDirection * knockbackDistance;
    }

}