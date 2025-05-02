using UnityEngine;

public class PlayerDetectionZone : MonoBehaviour //detection for the player to interact with the bookcase or the dungeon selector
{
    [SerializeField] private bool isForTomeMenu = false;

    private DungeonSelector dungeonSelector;

    void Start()
    {
        dungeonSelector = GetComponentInParent<DungeonSelector>();

        if (!isForTomeMenu && dungeonSelector == null)
        {
            Debug.LogError("No DungeonSelector found on parent object!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (isForTomeMenu)
            {
                var tomeMenu = FindObjectOfType<TomeAssignmentMenu>();
                if (tomeMenu != null)
                {
                    tomeMenu.SetBookcaseProximity(true);
                }
            }
            else if (dungeonSelector != null)
            {
                dungeonSelector.SetPlayerInRange(true);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (isForTomeMenu)
            {
                var tomeMenu = FindObjectOfType<TomeAssignmentMenu>();
                if (tomeMenu != null)
                {
                    tomeMenu.SetBookcaseProximity(false);
                }
            }
            else if (dungeonSelector != null)
            {
                dungeonSelector.SetPlayerInRange(false);
            }
        }
    }
}
