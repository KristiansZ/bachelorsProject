using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private GameObject damageFlashPanel;

    [Header("UI Settings")]
    [SerializeField] private float damageFlashDuration = 0.2f;

    private PlayerStatManager playerStats;
    private PlayerDamageHandler damageHandler;
    private CanvasGroup damageFlashGroup;

    private IEnumerator Start()
    {
        //need data from PlayerStatManager and PlayerDamageHandler for displaying
        while (playerStats == null || damageHandler == null)
        {
            playerStats = FindObjectOfType<PlayerStatManager>();
            damageHandler = FindObjectOfType<PlayerDamageHandler>();
            yield return null;
        }

        if (damageFlashPanel != null)
        {
            damageFlashGroup = damageFlashPanel.GetComponent<CanvasGroup>();
            if (damageFlashGroup == null)
                damageFlashGroup = damageFlashPanel.AddComponent<CanvasGroup>();

            damageFlashGroup.alpha = 0f;
        }

        damageHandler.OnDamageTakenEvent += HandleDamageTaken;
    }

    private void OnDestroy()
    {
        if (damageHandler != null)
        {
            damageHandler.OnDamageTakenEvent -= HandleDamageTaken;
        }
    }

    private void Update()
    {
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (playerStats == null || healthFillImage == null) return;

        float healthPercentage = playerStats.currentHealth / playerStats.maxHealth;
        healthFillImage.fillAmount = healthPercentage;

        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(playerStats.currentHealth)} / {playerStats.maxHealth}";
        }
    }

    private void HandleDamageTaken()
    {
        FlashDamageIndicator();
        StartCoroutine(PulseHealthOrb());
    }

    private void FlashDamageIndicator()
    {
        if (damageFlashPanel != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        damageFlashGroup.alpha = 0.6f;

        float elapsed = 0f;
        while (elapsed < damageFlashDuration)
        {
            elapsed += Time.deltaTime;
            damageFlashGroup.alpha = Mathf.Lerp(0.6f, 0f, elapsed / damageFlashDuration);
            yield return null;
        }

        damageFlashGroup.alpha = 0f;
    }

    private IEnumerator PulseHealthOrb()
    {
        if (healthFillImage == null) yield break;

        Vector3 originalScale = healthFillImage.transform.localScale;
        Vector3 pulseScale = originalScale * 1.05f; // Smaller pulse

        float pulseDuration = 0.2f;
        float elapsed = 0f;

        while (elapsed < pulseDuration / 2f)
        {
            elapsed += Time.deltaTime;
            healthFillImage.transform.localScale = Vector3.Lerp(originalScale, pulseScale, elapsed / (pulseDuration / 2f));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < pulseDuration / 2f)
        {
            elapsed += Time.deltaTime;
            healthFillImage.transform.localScale = Vector3.Lerp(pulseScale, originalScale, elapsed / (pulseDuration / 2f));
            yield return null;
        }

        healthFillImage.transform.localScale = originalScale;
    }
}
