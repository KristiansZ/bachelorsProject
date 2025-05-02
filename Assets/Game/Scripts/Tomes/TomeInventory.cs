using UnityEngine;

public class TomeInventory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TomeDisplay tomeDisplay;
    
    private TomeType[] _equippedTomes = new TomeType[3];

    private void Awake()
    {
        if (tomeDisplay == null)
        {
            tomeDisplay = FindObjectOfType<TomeDisplay>();
        }
        InitializeEmptySlots();
    }

    private void InitializeEmptySlots()
    {
        for (int i = 0; i < _equippedTomes.Length; i++)
        {
            UpdateTomeSlot(i, TomeType.None, tomeDisplay.emptySlotSprite);
        }
    }

   public void EquipTome(TomeType newTome, int targetSlotIndex)
    {
        if (!IsValidSlot(targetSlotIndex)) return;
        
        //find existing tome in other slot
        for (int i = 0; i < _equippedTomes.Length; i++)
        {
            if (_equippedTomes[i] == newTome)
            {
                //swap contents
                TomeType oldTome = _equippedTomes[targetSlotIndex];
                _equippedTomes[targetSlotIndex] = newTome;
                _equippedTomes[i] = oldTome;
                
                UpdateBothSlots(targetSlotIndex, i);
                return;
            }
        }
        
        //no duplicate found - normal equip
        _equippedTomes[targetSlotIndex] = newTome;
        UpdateTomeSlot(targetSlotIndex, newTome, GetTomeIcon(newTome));
        TomeController.Instance.AssignTomeToSlot(newTome, targetSlotIndex);
    }

    private void UpdateBothSlots(int slotA, int slotB)
    {
        UpdateTomeSlot(slotA, _equippedTomes[slotA], GetTomeIcon(_equippedTomes[slotA]));
        UpdateTomeSlot(slotB, _equippedTomes[slotB], GetTomeIcon(_equippedTomes[slotB]));
    }

    private void UpdateTomeSlot(int slotIndex, TomeType tomeType, Sprite icon)
    {
        tomeDisplay.UpdateTomeSlot(slotIndex, tomeType, icon);
    }

    public Sprite GetTomeIcon(TomeType tomeType)
    {
        if (tomeType == TomeType.None) 
            return tomeDisplay.emptySlotSprite;

        // Correct path - remove "Assets" and "Resources" from the path
        string spritePath = $"Sprites/{tomeType}"; 
        Sprite sprite = Resources.Load<Sprite>(spritePath);
        
        if (sprite == null)
        {
            Debug.LogError($"Tome sprite not found at: {spritePath}\n");
        }
        
        return sprite;
    }
    

    private bool IsValidSlot(int index) => index >= 0 && index < _equippedTomes.Length;
}