using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider slider;
    
    [Header("Settings")]
    [SerializeField] private float yOffset = 2f;
    [SerializeField] private bool alwaysFaceCamera = true;
    
    private Transform target;
    private Camera mainCam;

    public void Initialize(Transform enemyTransform, float maxHealth)
    {
        target = enemyTransform;
        mainCam = Camera.main;
        SetMaxHealth(maxHealth);
        UpdatePosition();
    }

    public void SetMaxHealth(float maxHealth)
    {
        slider.maxValue = maxHealth;
        slider.value = maxHealth;
    }

    public void UpdateHealth(float currentHealth)
    {
        slider.value = currentHealth;
        gameObject.SetActive(currentHealth > 0);
    }

    void LateUpdate()
    {
        if (target == null) 
        {
            gameObject.SetActive(false);
            return;
        }
        
        UpdatePosition();
        
        if (alwaysFaceCamera)
        {
            FaceCamera();
        }
    }

    private void UpdatePosition()
    {
        transform.position = target.position + Vector3.up * yOffset;
    }

    private void FaceCamera()
    {
        transform.rotation = mainCam.transform.rotation;
    }
}