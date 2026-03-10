using Photon.Pun;
using UnityEngine;

public class PlayerCombat : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private ActiveWeapon activeWeapon;
    [SerializeField] [Tooltip("How quickly remote weapon direction catches up to network state. Higher = snappier, lower = smoother.")]
    private float weaponInterpolationSpeed = 18f;

    private Vector2 _networkWeaponDirection;
    private bool _networkFacingLeft;
    private Vector2 _currentWeaponDirection;
    private bool _currentFacingLeft;
    private bool _hasReceivedWeaponState;

    private void Update()
    {
        if (photonView.IsMine)
        {
            activeWeapon.UpdatePlayerFacingDirection(CalculateMousePosToPlayer(), playerController.FacingLeft);
            return;
        }

        // Smoothly interpolate weapon direction between serialization updates (avoids choppy sword from 30 Hz sync)
        _currentWeaponDirection = Vector2.Lerp(_currentWeaponDirection, _networkWeaponDirection, weaponInterpolationSpeed * Time.deltaTime);
        _currentFacingLeft = _networkFacingLeft;
        activeWeapon.UpdatePlayerFacingDirection(_currentWeaponDirection, _currentFacingLeft);
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
