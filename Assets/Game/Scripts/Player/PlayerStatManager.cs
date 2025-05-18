using UnityEngine;
using System.Collections;
using KinematicCharacterController;
using UnityEngine.SceneManagement;

public class PlayerStatManager : MonoBehaviour
{
    [Header("Core Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float armour = 0f;
    public float baseMoveSpeed = 5f;
    public float currentMoveSpeed;
    public float lifeRegenerationAmount = 1f;
    public float lifeRegenerationInterval = 3f;

    [Header("References")]
    [SerializeField] private PlayerController playerController;

    private bool isDead = false;

    private void Awake()
    {
        currentHealth = maxHealth;
        currentMoveSpeed = baseMoveSpeed;
        StartCoroutine(LifeRegenerationCoroutine());

        if (playerController == null)
            playerController = GetComponent<PlayerController>();
        
        UpdateMovementSpeed();
    }

    private void Update()
    {
        if (!isDead && currentHealth <= 0)
        {
            isDead = true;
            StartCoroutine(FadeToLoseMenu());
        }
    }

    private IEnumerator FadeToLoseMenu()
    {
        ScreenFade fader = FindObjectOfType<ScreenFade>();
        if (fader != null)
            yield return StartCoroutine(fader.FadeOut());

        SceneManager.LoadScene("LoseMenuScene");
    }

    private IEnumerator LifeRegenerationCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(lifeRegenerationInterval);
            currentHealth = Mathf.Min(maxHealth, currentHealth + lifeRegenerationAmount);
        }
    }

    public void TakeDamage(float amount)
    {
        float damageTaken = Mathf.Max(1, amount - armour);
        currentHealth -= damageTaken;
    }

    private void UpdateMovementSpeed()
    {
        if (playerController != null)
        {
            playerController.MoveSpeed = currentMoveSpeed;
        }
    }

    //player stat (global) upgrade methods
    public void IncreaseHealth(float amount)
    {
        maxHealth += amount;
        currentHealth += amount;
    }

    public void IncreaseArmour(float amount) => armour += amount;

    public void IncreaseMovementSpeed(float amount) //not used in game yet
    {
        currentMoveSpeed = currentMoveSpeed + amount;
        UpdateMovementSpeed();
    }

    public void IncreaseLifeRegenerationAmount(float amount) => lifeRegenerationAmount += amount;

    public void IncreaseLifeRegenerationSpeed(float amount)
    {
        lifeRegenerationInterval = Mathf.Max(0.1f, lifeRegenerationInterval - amount);
    }
}