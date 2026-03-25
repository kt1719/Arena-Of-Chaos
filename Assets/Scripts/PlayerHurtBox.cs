using UnityEngine;

public class PlayerHurtBox : MonoBehaviour
{
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private PlayerKnockback _playerKnockback;

    public PlayerKnockback GetPlayerKnockback() => _playerKnockback;
    public PlayerMovement GetPlayerMovement() => _playerMovement;
}
