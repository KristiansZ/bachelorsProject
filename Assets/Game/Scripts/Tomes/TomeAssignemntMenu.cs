using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class TomeAssignmentMenu : MonoBehaviour
{
    [Header("UI References")]
    public GameObject tomeButtonPrefab;
    public Transform tomeMenuPanel;
    public GameObject background;

    [Header("Data")]
    [SerializeField] public TomeInventory tomeInventory;

    public List<TomeType> availableTomes = new List<TomeType>();

    private bool isMenuOpen = false;
    private bool isNearBookcase = false;

    private void Start()
    {
        availableTomes.Clear();
        availableTomes.Add(TomeType.None);
        foreach (TomeType tome in System.Enum.GetValues(typeof(TomeType)))
        {
            if (tome != TomeType.None) availableTomes.Add(tome);
        }
        
        tomeMenuPanel.gameObject.SetActive(false);
        background.SetActive(false);
        isMenuOpen = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && isNearBookcase)
        {
            ToggleMenu();
        }
    }

    public void SetBookcaseProximity(bool inRange)
    {
        isNearBookcase = inRange;
        
        if (!inRange && isMenuOpen)
        {
            CloseMenu();
        }
    }

    public void RefreshTomeButtons()
    {
        foreach (Transform child in tomeMenuPanel)
        {
            Destroy(child.gameObject);
        }

        foreach (TomeType tomeType in TomeController.Instance.GetAvailableTomeTypes())
        {
            CreateTomeButton(tomeType);
        }
    }

    private void ToggleMenu()
    {
        if (isMenuOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    public void OpenMenu()
    {
        tomeMenuPanel.gameObject.SetActive(true);

        if (background != null) background.SetActive(true);
        
        foreach (Transform child in tomeMenuPanel)
        {
            Destroy(child.gameObject);
        }

        foreach (TomeType tome in availableTomes)
        {
            CreateTomeButton(tome);
        }

        isMenuOpen = true;
    }

    public void CloseMenu()
    {
        tomeMenuPanel.gameObject.SetActive(false);
        if (background != null) background.SetActive(false);
        isMenuOpen = false;
        
        foreach (Transform child in tomeMenuPanel)
        {
            Destroy(child.gameObject);
        }
    }

    private void CreateTomeButton(TomeType tome)
    {
        GameObject btn = Instantiate(tomeButtonPrefab, tomeMenuPanel);
    
        TMPro.TextMeshProUGUI tmpComponent = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpComponent != null)
        {
            tmpComponent.text = FormatTomeName(tome);
        }

        Transform iconTransform = btn.transform.Find("Icon");
        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            iconImage.sprite = tomeInventory.GetTomeIcon(tome);

            DraggableTome draggable = iconTransform.GetComponent<DraggableTome>();
            if (draggable == null)
            {
                draggable = iconTransform.gameObject.AddComponent<DraggableTome>();
            }
            draggable.tomeType = tome;
        }
        else
        {
            Debug.LogError("Icon child not found in tome button prefab!");
        }
    }
    private string FormatTomeName(TomeType tome)
    {
        if (tome == TomeType.None) return "";
        
        string rawName = tome.ToString();
        System.Text.StringBuilder formatted = new System.Text.StringBuilder();
        
        if (rawName.Length > 0)
            formatted.Append(rawName[0]);
        
        for (int i = 1; i < rawName.Length; i++) //space before each capital letter
        {
            if (char.IsUpper(rawName[i]))
            {
                formatted.Append(" ");
            }
            formatted.Append(rawName[i]);
        }
        
        return formatted.ToString().Replace(" Tome", "");//remove "Tome"
    }
}