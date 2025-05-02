using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TomeDisplay : MonoBehaviour
{
    [System.Serializable]
    public class TomeSlot
    {
        public Image icon;
        public Image cooldownOverlay;
        public Outline outline;
        
        [HideInInspector] public TomeType currentTome;
    }

    [Header("Slot Configuration")]
    [SerializeField] private TomeSlot[] slots = new TomeSlot[3];
    [SerializeField] public Sprite emptySlotSprite;

    [Header("Visual Settings")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);
    [SerializeField] private Color cooldownColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float pulseDuration = 0.3f;

    private int currentSelectedIndex = 0;

    private void Awake()
    {
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        foreach (var slot in slots)
        {
            if (slot.cooldownOverlay != null)
            {
                slot.cooldownOverlay.type = Image.Type.Filled;
                slot.cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
                slot.cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
                slot.cooldownOverlay.color = cooldownColor;
                slot.cooldownOverlay.fillAmount = 0;
            }

            if (slot.outline != null)
            {
                slot.outline.effectColor = Color.clear;
            }
        }
        
        SetSelectedSlot(0);
    }

    public void UpdateTomeSlot(int slotIndex, TomeType tomeType, Sprite icon)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) return;
        
        slots[slotIndex].currentTome = tomeType;
        slots[slotIndex].icon.sprite = icon ?? emptySlotSprite;
        slots[slotIndex].icon.enabled = (icon != null);
        
        if (tomeType != TomeType.None)
            StartCoroutine(PulseSlot(slotIndex));
    }
    public void OnTomeDropped(int slotIndex, TomeType tomeType)
    {
        TomeController.Instance.AssignTomeToSlot(tomeType, slotIndex);
    }

    public void SetSelectedSlot(int selectedIndex)
    {
        currentSelectedIndex = selectedIndex;
        for (int i = 0; i < slots.Length; i++)
        {
            bool isSelected = (i == selectedIndex);
            
            if (slots[i].outline != null)
                slots[i].outline.effectColor = isSelected ? Color.yellow : new Color(0.5f, 0.5f, 0.5f);
        }
    }

    public void UpdateCooldown(int slotIndex, float cooldownProgress)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) return;
        
        if (slots[slotIndex].cooldownOverlay != null)
        {
            slots[slotIndex].cooldownOverlay.fillAmount = cooldownProgress;
            slots[slotIndex].cooldownOverlay.gameObject.SetActive(cooldownProgress > 0);
        }
    }

    //pulse animation when a tome is assigned to a slot
    private IEnumerator PulseSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) yield break;
        
        Image icon = slots[slotIndex].icon;
        Vector3 originalScale = icon.transform.localScale;
        float elapsed = 0f;

        while (elapsed < pulseDuration / 2)
        {
            float t = elapsed / (pulseDuration / 2);
            float scale = Mathf.Lerp(1f, pulseScale, t);
            icon.transform.localScale = originalScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < pulseDuration / 2)
        {
            float t = elapsed / (pulseDuration / 2);
            float scale = Mathf.Lerp(pulseScale, 1f, t);
            icon.transform.localScale = originalScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        //ensure exact original scale (had some issues where scale just kept getting bigger)
        icon.transform.localScale = originalScale;
    }
}