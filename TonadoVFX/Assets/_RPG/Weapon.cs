using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "ARPG/Weapon")]
public class Weapon : MonoBehaviour
{
    public event System.Action<Collider> OnWeaponHit;
    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Enemy"))
        {
            OnWeaponHit?.Invoke(other);
            Debug.Log("Weapon hit: " + other.name);
        }
    }
    #region Weapon Type Enum
    public enum WeaponType
    {
        Sword,
        Axe,
        Spear,
        Bow,
        Staff
    }
    #endregion

    #region Settings
    public string weaponName;
    public WeaponType weaponType;
    public float attackRange = 2f;
    public float damageMultiplier = 1f;
    public float attackSpeedMultiplier = 1f;
    #endregion
}