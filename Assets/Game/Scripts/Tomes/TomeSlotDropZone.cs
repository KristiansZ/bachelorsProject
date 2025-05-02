using UnityEngine;
using UnityEngine.EventSystems;

public class TomeSlotDropZone : MonoBehaviour, IDropHandler
{
    public int slotIndex;
    
    private TomeInventory inventory;
    
    private void Start()
    {
        inventory = FindObjectOfType<TomeInventory>();
        if (inventory == null)
        {
            Debug.LogError("No TomeInventory found in scene!", this);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (inventory == null) return;

        //get the DraggableTome
        DraggableTome draggable = eventData.pointerDrag.GetComponentInParent<DraggableTome>();
        if (draggable != null)
        {
            //handle tome assignment
            inventory.EquipTome(draggable.tomeType, slotIndex);
            TomeController.Instance.AssignTomeToSlot(draggable.tomeType, slotIndex);

            var draggableTomeScript = draggable.GetComponent<DraggableTome>();
            if (draggableTomeScript != null)
            {
                draggableTomeScript.OnEndDrag(eventData);//call OnEndDrag to ensure proper cleanup
            }
        }
    }
}