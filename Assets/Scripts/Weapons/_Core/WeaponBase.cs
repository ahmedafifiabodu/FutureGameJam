using UnityEngine;

/// <summary>
/// Base class for all weapons (ranged and melee)
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Info")]
    [SerializeField] protected string weaponName = "Weapon";
    [SerializeField] protected float damage = 10f;
    
    [Header("Animation")]
    [SerializeField] protected Animator weaponAnimator;
    
    protected bool isEquipped = false;
    protected InputManager inputManager;

    public virtual void Initialize(InputManager input)
    {
        inputManager = input;
    }

    public virtual void Equip()
    {
        isEquipped = true;
        gameObject.SetActive(true);
    }

    public virtual void Unequip()
    {
        isEquipped = false;
        gameObject.SetActive(false);
    }

    public abstract void Attack();
    public abstract void Update();
    
    public string GetWeaponName() => weaponName;
    public float GetDamage() => damage;
}
