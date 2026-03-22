using Fusion;
using UnityEngine;

public class SwordVisual : MonoBehaviour
{
    private static readonly int ATTACK_TRIGGER_STRING_HASH = Animator.StringToHash("Attack");

    [SerializeField] private SwordWeapon swordWeapon;
    [SerializeField] private NetworkMecanimAnimator _networkAnimator;

    private void Start()
    {
        swordWeapon.OnSwordSwipe += PlaySwipeAnimation;
    }

    private void PlaySwipeAnimation(SwordSwipe swipe)
    {
        _networkAnimator.SetTrigger(ATTACK_TRIGGER_STRING_HASH, true); // https://doc.photonengine.com/fusion/current/manual/sync-components/network-mecanim-animator#:~:text=passThroughOnInputAuthority
    }
}
