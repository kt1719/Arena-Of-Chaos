public interface IWeapon
{
    public void SetPlayer(PlayerController player);
    public void Attack();
    public void InstantiateWeapon();
    public float GetWeaponCD();
}
