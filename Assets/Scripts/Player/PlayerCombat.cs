using System;
using Photon.Pun;
using UnityEngine;

public class PlayerCombat : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private ActiveWeapon activeWeapon;
    private float weaponInterpolationSpeed = 18f;

    private Vector2 _networkWeaponDirection;
    private bool _networkFacingLeft;
    private Vector2 _currentWeaponDirection;
    private bool _currentFacingLeft;
    private bool _hasReceivedWeaponState;
    private bool _isAttacking;

    private void Start() {
        GameInput.Instance.OnPlayerAttack += OnPlayerAttack;
        GameInput.Instance.OnPlayerCancelAttack += OnPlayerCancelAttack;
    }

    private void OnPlayerAttack()
    {
        _isAttacking = true;
    }

    private void OnPlayerCancelAttack()
    {
        _isAttacking = false;
    }

    private void Update()
    {
        if (photonView.IsMine)
        {
            AttackLocal();
            activeWeapon.UpdatePlayerFacingDirection(CalculateMousePosToPlayer(), playerController.FacingLeft);
            return;
        }

        // Smoothly interpolate weapon direction between serialization updates (avoids choppy sword from 30 Hz sync)
        _currentWeaponDirection = Vector2.Lerp(_currentWeaponDirection, _networkWeaponDirection, weaponInterpolationSpeed * Time.deltaTime);
        _currentFacingLeft = _networkFacingLeft;
        activeWeapon.UpdatePlayerFacingDirection(_currentWeaponDirection, _currentFacingLeft);
    }

    private void AttackLocal()
    {
        if (_isAttacking)
        {
            byte currentWeaponState = activeWeapon.GetCurrentWeaponState();
            bool success = activeWeapon.Attack();
            if (success)
            {
                photonView.RPC(nameof(AttackRemote), RpcTarget.Others, currentWeaponState);
            }
        }
    }

    [PunRPC]
    private void AttackRemote(byte currentWeaponState)
    {
        activeWeapon.UpdateWeaponState(currentWeaponState);
        activeWeapon.Attack(remoteServerAttack:true);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(CalculateMousePosToPlayer());
            stream.SendNext(playerController.FacingLeft);
        }
        else
        {
            _networkWeaponDirection = (Vector2)stream.ReceiveNext();
            _networkFacingLeft = (bool)stream.ReceiveNext();
            if (!_hasReceivedWeaponState)
            {
                _currentWeaponDirection = _networkWeaponDirection;
                _hasReceivedWeaponState = true;
            }
            _currentFacingLeft = _networkFacingLeft;
        }
    }

    private Vector2 CalculateMousePosToPlayer()
    {
        Vector2 mouseWorldPos = GameInput.Instance.GetMouseInputWorldPos();
        return mouseWorldPos - (Vector2)transform.position;
    }
}
