using Fusion;
using UnityEngine;

public class BowVisual : MonoBehaviour
{
    private static readonly int ATTACK_TRIGGER_HASH = Animator.StringToHash("Attack");

    [SerializeField] private BowWeapon bowWeapon;
    [SerializeField] private NetworkMecanimAnimator _networkAnimator;

    private void Start()
    {
        bowWeapon.OnBowShoot += PlayShootAnimation;
    }

    private void PlayShootAnimation()
    {
        _networkAnimator.SetTrigger(ATTACK_TRIGGER_HASH, true);
    }
}
