using System;
using UnityEngine;

public class SwordWeaponVisual : MonoBehaviour
{
    private static readonly int ATTACK_TRIGGER_STRING_HASH = Animator.StringToHash("Attack");

    [SerializeField] private Transform swordCollider;
    [SerializeField] private SwordWeapon swordWeapon;

    private Animator animator;
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        swordWeapon.OnPlayerAttack += SwordWeapon_OnPlayerAttack;

        swordCollider.gameObject.SetActive(false);
    }

    private void SwordWeapon_OnPlayerAttack()
    {
        animator.SetTrigger(ATTACK_TRIGGER_STRING_HASH);
        Debug.Log("Setting sword collider active");
        swordCollider.gameObject.SetActive(true);
    }

    public void DoneAttackingAnimEvent() // Used in sword slash animation
    {
        swordCollider.gameObject.SetActive(false);
    }
}
