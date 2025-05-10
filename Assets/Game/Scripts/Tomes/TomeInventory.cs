using UnityEngine;

public class TomeInventory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TomeDisplay tomeDisplay;
    
    private TomeType[] equippedTomes = new TomeType[3];

    [Header("Audio")]
    [SerializeField] private AudioClip switchTomeSFX;
    [SerializeField] private float switchVolume = 0.7f;
    private AudioSource audioSource;

    private void Awake()
    {
        if (tomeDisplay == null)
        {
            tomeDisplay = FindObjectOfType<TomeDisplay>();
        }
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
        InitializeEmptySlots();
    }

    private void InitializeEmptySlots()
    {
        for (int i = 0; i < equippedTomes.Length; i++)
        {
            UpdateTomeSlot(i, TomeType.None, tomeDisplay.emptySlotSprite);
        }
    }

   public void EquipTome(TomeType newTome, int targetSlotIndex)
    {
        if (!IsValidSlot(targetSlotIndex)) return;
        
        //find existing tome in other slot
        for (int i = 0; i < equippedTomes.Length; i++)
        {
            if (equippedTomes[i] == newTome)
            {
                //swap contents
                TomeType oldTome = equippedTomes[targetSlotIndex];
                equippedTomes[targetSlotIndex] = newTome;
                equippedTomes[i] = oldTome;
                
                UpdateBothSlots(targetSlotIndex, i);
                PlaySwitchTomeSound();
                return;
            }
        }
        
        //no duplicate found - normal equip
        equippedTomes[targetSlotIndex] = newTome;
        UpdateTomeSlot(targetSlotIndex, newTome, GetTomeIcon(newTome));
        TomeController.Instance.AssignTomeToSlot(newTome, targetSlotIndex);
        PlaySwitchTomeSound();
    }

    private void UpdateBothSlots(int slotA, int slotB)
    {
        UpdateTomeSlot(slotA, equippedTomes[slotA], GetTomeIcon(equippedTomes[slotA]));
        UpdateTomeSlot(slotB, equippedTomes[slotB], GetTomeIcon(equippedTomes[slotB]));
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

    private void PlaySwitchTomeSound()
    {
        if (switchTomeSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(switchTomeSFX, switchVolume);
        }
    }
    

    private bool IsValidSlot(int index) => index >= 0 && index < equippedTomes.Length;
}