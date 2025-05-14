using UnityEngine;
using System.Collections.Generic;

public class TomeController : MonoBehaviour
{
    public static TomeController Instance;

    [Header("References")]
    [SerializeField] public TomeDisplay tomeDisplay;
    [SerializeField] public Transform tomeInventoryParent;
    [SerializeField] public TomeAssignmentMenu tomeAssignmentMenu;

    [Header("Tome Data")]
    public List<Tome> allTomePrefabs = new List<Tome>(); //all possible tomes
    public Tome blankTomePrefab; //blankTome prefab reference
    private Tome[] equippedTomes = new Tome[3]; //currently equipped
    private int activeTomeIndex = 0;
    
    [Header("Upgrade Settings")]
    [SerializeField] public float globalDamageBonus = 0f;

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeTomes();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitializeTomes()
    {
        for (int i = 0; i < 3; i++)//equip BlankTomes by default at the start
        {
            EquipTome(blankTomePrefab, i);
        }
    }

    public void EquipTome(Tome tomePrefab, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 2) return;

        //remove old tome if exists
        if (equippedTomes[slotIndex] != null)
        {
            Destroy(equippedTomes[slotIndex].gameObject);
        }

        //create and equip new tome
        Tome newTome = Instantiate(tomePrefab, transform);
        newTome.gameObject.SetActive(slotIndex == activeTomeIndex);
        equippedTomes[slotIndex] = newTome;
        
        //apply any global damage bonus to the new tome
        if (newTome != null)
        {
            newTome.damageMultiplier += globalDamageBonus;
        }

        //update UI display
        tomeDisplay.UpdateTomeSlot(slotIndex, newTome.tomeType, newTome.GetIcon());
        tomeAssignmentMenu.RefreshTomeButtons();
    }

    public void SetActiveTome(int index)
    {
        if (equippedTomes[activeTomeIndex] != null)
            equippedTomes[activeTomeIndex].gameObject.SetActive(false);

        activeTomeIndex = Mathf.Clamp(index, 0, 2);

        if (equippedTomes[activeTomeIndex] != null)
            equippedTomes[activeTomeIndex].gameObject.SetActive(true);

        tomeDisplay.SetSelectedSlot(activeTomeIndex);
    }

    //for UI to get available tomes
    public List<TomeType> GetAvailableTomeTypes()
    {
        List<TomeType> types = new List<TomeType>();
        foreach (Tome tome in allTomePrefabs)
        {
            types.Add(tome.tomeType);
        }
        return types;
    }

    //for drag-and-drop assignment
    public void AssignTomeToSlot(TomeType tomeType, int slotIndex)
    {
        //find if the tome is already equipped
        int existingIndex = -1;
        for (int i = 0; i < equippedTomes.Length; i++)
        {
            if (equippedTomes[i] != null && equippedTomes[i].tomeType == tomeType)
            {
                existingIndex = i;
                break;
            }
        }

        //prevent duplicate tomes in multiple slots
        if (existingIndex >= 0 && existingIndex != slotIndex)
        {
            //swap slots
            Tome temp = equippedTomes[slotIndex];
            equippedTomes[slotIndex] = equippedTomes[existingIndex];
            equippedTomes[existingIndex] = temp;

            //swap active state
            equippedTomes[slotIndex].gameObject.SetActive(slotIndex == activeTomeIndex);
            equippedTomes[existingIndex].gameObject.SetActive(existingIndex == activeTomeIndex);

            //update display
            tomeDisplay.UpdateTomeSlot(slotIndex, equippedTomes[slotIndex].tomeType, equippedTomes[slotIndex].GetIcon());
            tomeDisplay.UpdateTomeSlot(existingIndex, equippedTomes[existingIndex].tomeType, equippedTomes[existingIndex].GetIcon());
        }
        else
        {
            //normal equip (not a duplicate)
            Tome tomePrefab = allTomePrefabs.Find(t => t.tomeType == tomeType);
            if (tomePrefab != null)
            {
                EquipTome(tomePrefab, slotIndex);
            }
        }

        tomeAssignmentMenu.RefreshTomeButtons();
    }
    
    public void ApplyGlobalDamageBonus(float bonus)
    {
        if (equippedTomes == null)
        {
            Debug.LogError("equippedTomes is null!");
            return;
        }

        globalDamageBonus += bonus;

        //apply damage
        foreach (var tome in equippedTomes)
        {
            tome.damageMultiplier += bonus;
        }
    }

    public void ApplyGlobalAttackSpeed(float bonus)
{
    if (equippedTomes == null)
    {
        Debug.LogError("equippedTomes is null!");
        return;
    }

    float cooldownReductionPercent = 1f - bonus;

    //iterate through equipped tomes and apply cooldown bonus
    foreach (var tome in equippedTomes)
    {
        if (tome != null)
        {
            tome.UpgradeCooldown(cooldownReductionPercent);
        }
    }
}

    public void ApplyTomeUpgrade(TomeType tomeType, UpgradeType upgradeType, float value)
    {
        Tome tome = null;
        for (int i = 0; i < equippedTomes.Length; i++) 
        {
            if (equippedTomes[i] != null && equippedTomes[i].tomeType == tomeType)
            {
                tome = equippedTomes[i];
                break;
            }
        }
        
        if (tome == null) return;

        switch (upgradeType)
        {
            case UpgradeType.ProjectileSpeed:
                if (tome is FireballTome fireballTomeSpeed)
                {
                    fireballTomeSpeed.stats.projectileSpeed *= (1f + value);
                }
                break;

            case UpgradeType.ProjectileSize:
                if (tome is FireballTome fireballTomeSize)
                {
                    fireballTomeSize.UpgradeProjectileSize(value);
                }
                break;

            case UpgradeType.ProjectileCount:
                if (tome is FireballTome fireballTomeProjCount)
                {
                    fireballTomeProjCount.AddProjectile();
                }
                break;
                    
            case UpgradeType.AreaOfEffect:
                if (tome is MeteoriteTome meteoriteTomeArea)
                {
                    meteoriteTomeArea.UpgradeImpactRadius(value);
                }
                break;
                
            case UpgradeType.DamageOverTime:
                if (tome is MeteoriteTome meteoriteTomeDOT)
                {
                    meteoriteTomeDOT.UpgradeBurnDamage(value);
                }
                break;
                
            //more cases for more upgrades
        }
    }
}