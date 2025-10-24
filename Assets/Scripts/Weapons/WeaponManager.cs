using UnityEngine;

/// <summary>
/// Manages weapon switching and equipping for the host _controller.
/// Supports both ranged and melee weapons.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Slots")]
    [SerializeField] private WeaponBase primaryWeapon;

    [SerializeField] private WeaponBase secondaryWeapon;

    [Header("Weapon Holder")]
    [SerializeField] private Transform weaponHolder; // Where weapons are parented

    [Header("Animation")]
    [SerializeField] private Animator weaponAnimator; // Character animator for weapon switching

    private WeaponBase currentWeapon;
    private InputManager inputManager;
    private bool isEnabled = false;
    private int changingWeaponHash;

    private void Awake()
    {
        // Cache animation parameter hash
        changingWeaponHash = Animator.StringToHash(GameConstant.AnimationParameters.ChangingWeapon);
    }

    public void Initialize(InputManager input)
    {
        inputManager = input;

        // Initialize all weapons
        if (primaryWeapon)
        {
            primaryWeapon.Initialize(inputManager);
            primaryWeapon.Unequip();
        }

        if (secondaryWeapon)
        {
            secondaryWeapon.Initialize(inputManager);
            secondaryWeapon.Unequip();
        }

        // Equip primary by default
        if (primaryWeapon)
        {
            EquipWeapon(primaryWeapon);
        }
    }

    public void Enable()
    {
        isEnabled = true;
        if (currentWeapon)
        {
            currentWeapon.Equip();
        }
    }

    public void Disable()
    {
        isEnabled = false;
        if (currentWeapon)
        {
            currentWeapon.Unequip();
        }
    }

    private void Update()
    {
        if (!isEnabled || inputManager == null) return;

        // Handle weapon switching
        if (inputManager.PlayerActions.Previous.triggered)
        {
            SwitchToWeapon(primaryWeapon);
        }

        if (inputManager.PlayerActions.Next.triggered)
        {
            SwitchToWeapon(secondaryWeapon);
        }

        // Update current weapon
        if (currentWeapon)
        {
            currentWeapon.Update();
        }
    }

    private void EquipWeapon(WeaponBase weapon)
    {
        if (weapon == null) return;

        // Unequip current weapon
        if (currentWeapon)
        {
            currentWeapon.Unequip();
        }

        // Equip new weapon
        currentWeapon = weapon;
        currentWeapon.Equip();

        Debug.Log($"[WeaponManager] Equipped {currentWeapon.GetWeaponName()}");
    }

    private void SwitchToWeapon(WeaponBase weapon)
    {
        if (weapon == null || weapon == currentWeapon) return;

        // Trigger weapon change animation if character animator exists
        if (weaponAnimator)
        {
            weaponAnimator.SetTrigger(changingWeaponHash);
        }

        EquipWeapon(weapon);
    }

    // Public getters
    public WeaponBase GetCurrentWeapon() => currentWeapon;

    public WeaponBase GetPrimaryWeapon() => primaryWeapon;

    public WeaponBase GetSecondaryWeapon() => secondaryWeapon;

    // Assign weapons at runtime
    public void SetPrimaryWeapon(WeaponBase weapon)
    {
        primaryWeapon = weapon;
        if (weapon)
        {
            weapon.Initialize(inputManager);
            weapon.transform.SetParent(weaponHolder);
            weapon.Unequip();
        }
    }

    public void SetSecondaryWeapon(WeaponBase weapon)
    {
        secondaryWeapon = weapon;
        if (weapon)
        {
            weapon.Initialize(inputManager);
            weapon.transform.SetParent(weaponHolder);
            weapon.Unequip();
        }
    }
}