using UnityEngine;
using System.Collections.Generic;

public class WallFadeHandler : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private Material fadeMaterial;

    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private List<Renderer> currentlyFading = new List<Renderer>();
    private Transform target;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
        else
        {
            Debug.LogWarning("No GameObject tagged 'Player' found.");
        }
    }

    void Update()
    {
        if (target == null || Camera.main == null)
        {
            Debug.LogWarning("Missing target or Main Camera!");
            return;
        }

        Vector3 camPos = Camera.main.transform.position;
        Vector3 targetPos = target.position;
        Vector3 dir = (targetPos - camPos).normalized;
        float dist = Vector3.Distance(targetPos, camPos);

        //clear previously faded
        foreach (var rend in currentlyFading)
        {
            if (rend != null && originalMaterials.ContainsKey(rend))
                rend.materials = originalMaterials[rend];
        }
        currentlyFading.Clear();

        //raycast and change materials
        RaycastHit[] hits = Physics.RaycastAll(camPos, dir, dist, obstacleMask);
        foreach (var hit in hits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                if (!originalMaterials.ContainsKey(rend))
                    originalMaterials[rend] = rend.materials;

                Material[] fadedMats = new Material[rend.materials.Length];
                for (int i = 0; i < fadedMats.Length; i++)
                    fadedMats[i] = fadeMaterial;

                rend.materials = fadedMats;
                currentlyFading.Add(rend);
            }
        }
    }
}