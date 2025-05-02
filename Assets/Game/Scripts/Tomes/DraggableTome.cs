using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableTome : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TomeType tomeType;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private GameObject dragIcon;
    private Vector3 startPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //create drag icon (same as original, just gonna be dragged)
        dragIcon = new GameObject("Drag Icon");
        dragIcon.transform.SetParent(transform.root, false);
        dragIcon.transform.SetAsLastSibling();

        //add image
        var image = dragIcon.AddComponent<Image>();
        image.sprite = GetComponent<Image>().sprite;
        image.raycastTarget = false;
        image.preserveAspect = true;

        //set position and size
        dragIcon.transform.position = transform.position;
        dragIcon.GetComponent<RectTransform>().sizeDelta = rectTransform.sizeDelta;

        //store original position
        startPosition = rectTransform.position;

        //make origina kinda transparent
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            dragIcon.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }

        rectTransform.position = startPosition; //dragging ends, return to original position and not transparent
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }
}