using UnityEngine;

public class LampProximityController : MonoBehaviour
{
    [Header("Colour Settings")]
    public Color defaultColour;
    public Color activatedColour;
    public float colorTransitionSpeed = 3.0f;
    
    [Header("Player Detection")]
    public string playerTag = "Player";
    public float detectionRadius = 5.0f;
    public LayerMask playerLayerMask = -1;
    public Light lampLight;
    
    private Color currentColour;
    private Transform playerTransform;
    private bool isPlayerNearby = false;
    
    private void Start()
    {
        //assign light reference
        if (lampLight == null)
        {
            lampLight = GetComponentInChildren<Light>();
            
            if (lampLight == null)
            {
                Debug.LogError("No Light component found on " + gameObject.name);
                enabled = false;
                return;
            }
        }
        
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        currentColour = defaultColour;
        lampLight.color = defaultColour;
    }
    
    private void Update()
    {
        CheckPlayerProximity();
        
        if (isPlayerNearby && currentColour != activatedColour)
        {
            currentColour = Color.Lerp(currentColour, activatedColour, Time.deltaTime * colorTransitionSpeed);
            lampLight.color = currentColour;
        }
    }
    
    private void CheckPlayerProximity()
    {  
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        isPlayerNearby = distanceToPlayer <= detectionRadius;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}