using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "ARPG/Weapon")]
public class Weapon : MonoBehaviour
{
    public string weaponName;
    public WeaponType weaponType;
    public float attackRange = 2f;
    public float damageMultiplier = 1f;
    public float attackSpeedMultiplier = 1f;
    
    public enum WeaponType
    {
        Sword,
        Axe,
        Spear,
        Bow,
        Staff
    }
}