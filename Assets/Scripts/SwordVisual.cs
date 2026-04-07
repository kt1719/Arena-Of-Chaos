using Fusion;
using UnityEngine;

public class SwordVisual : MonoBehaviour
{
    private static readonly int ATTACK_TRIGGER_STRING_HASH_UP = Animator.StringToHash("AttackUp");
    private static readonly int ATTACK_TRIGGER_STRING_HASH_DOWN = Animator.StringToHash("AttackDown");

    [SerializeField] private SwordWeapon swordWeapon;
    [SerializeField] private NetworkMecanimAnimator _networkAnimator;

    private void Start()
    {
        swordWeapon.OnSwordSwipe += PlaySwipeAnimation;
    }

    private void PlaySwipeAnimation(SwordSwipe swipe)
    {
        switch (swipe) {
            case (SwordSwipe.DOWN):
                _networkAnimator.SetTrigger(ATTACK_TRIGGER_STRING_HASH_DOWN, true); // https://doc.photonengine.com/fusion/current/manual/sync-components/network-mecanim-animator#:~:text=passThroughOnInputAuthority
                break;
            case (SwordSwipe.UP):
                _networkAnimator.SetTrigger(ATTACK_TRIGGER_STRING_HASH_UP, true);
                break;
            
        }
    }
}
