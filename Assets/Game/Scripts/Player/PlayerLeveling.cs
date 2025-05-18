using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PlayerLeveling : MonoBehaviour
{
    [System.Serializable]
    public class TomeUpgrade
    {
        public string upgradeName;
        public UpgradeType upgradeType;
        public float floatValue;
        public int intValue;
        public TomeType affectedTome;
    }

    [Header("Experience Settings")]
    public float currentExperience = 0f;
    public int currentLevel = 1;
    [SerializeField] private float baseExperiencePerLevel = 100f;
    [SerializeField] private float experienceGrowthFactor = 1.2f;

    [Header("UI References")]
    [SerializeField] public TextMeshProUGUI upgradeText;
    [SerializeField] public TextMeshProUGUI experienceText;
    [SerializeField] public RectTransform experienceBar;
    [SerializeField] private float messageDuration = 3f;
    [SerializeField] public UpgradeSelectionUI upgradeSelectionUI;

    [Header("Systems")]
    [SerializeField] public PlayerStatManager playerStats;
    [SerializeField] public TomeController tomeController;

    private List<TomeUpgrade> availableUpgrades = new List<TomeUpgrade>();
    private float maxBarWidth;

    private void Awake()
    {
        InitializeUpgradePool();

        if (experienceBar != null)
        {
            if (experienceBar.parent != null && experienceBar.parent is RectTransform parentRect)
            {
                maxBarWidth = parentRect.rect.width;
            }
            else
            {
                maxBarWidth = 300f; //fallback for xp bar
            }
        }

        UpdateExperienceUI();
    }

    private void InitializeUpgradePool()
    {
        // global upgrades
        availableUpgrades.Add(new TomeUpgrade
        {
            upgradeName = "Max Health +20",
            upgradeType = UpgradeType.PlayerHealth,
            floatValue = 20f,
            affectedTome = TomeType.None
        });

        availableUpgrades.Add(new TomeUpgrade
        {
            upgradeName = "Armour +2",
            upgradeType = UpgradeType.PlayerArmour,
            floatValue = 2f,
            affectedTome = TomeType.None
        });

        availableUpgrades.Add(new TomeUpgrade
        {
            upgradeName = "All Tomes Damage +10%",
            upgradeType = UpgradeType.GlobalDamage,
            floatValue = 0.1f,
            affectedTome = TomeType.None
        });

        availableUpgrades.Add(new TomeUpgrade
        {
            upgradeName = "All Tomes 5% faster Attack Speed",
            upgradeType = UpgradeType.GlobalAttackSpeed,
            floatValue = 0.95f,
            affectedTome = TomeType.None
        });

        availableUpgrades.Add(new TomeUpgrade
        {
            upgradeName = "Life Regeneration +1",
            upgradeType = UpgradeType.LifeRegenAdd,
            floatValue = 1f,
            affectedTome = TomeType.None
        });

        availableUpgrades.Add(new TomeUpgrade
        {
            upgradeName = "Faster Life Regeneration -0.1s",
            upgradeType = UpgradeType.LifeRegenRate,
            floatValue = 0.1f,
            affectedTome = TomeType.None
        });

        // Fireball Tome 
        availableUpgrades.Add(new TomeUpgrade
        {
            upgradeName = "Fireball Size +20%",
            upgradeType = UpgradeType.ProjectileSize,
            floatValue = 0.2f,
            affectedTome = TomeType.FireballTome
        });

        availableUpgrades.Add(new TomeUpgrade
        {
            upgradeName = "Fireball Speed +15%",
            upgradeType = UpgradeType.ProjectileSpeed,
            floatValue = 0.15f,
            affectedTome = TomeType.FireballTome
        });

        availableUpgrades.Add(new TomeUpgrade
        {
            upgradeName = "Fireball +1 Projectile",
            upgradeType = UpgradeType.ProjectileCount,
            floatValue = 1f,
            affectedTome = TomeType.FireballTome
        });

        // Meteorite Tome
        availableUpgrades.Add(new TomeUpgrade
        {
            upgradeName = "Meteor AoE +20%",
            upgradeType = UpgradeType.AreaOfEffect,
            floatValue = 0.2f,
            affectedTome = TomeType.MeteoriteTome
        });

        availableUpgrades.Add(new TomeUpgrade
        {
            upgradeName = "Meteor Burn Damage +5 DPS",
            upgradeType = UpgradeType.DamageOverTime,
            floatValue = 5f,
            affectedTome = TomeType.MeteoriteTome
        });
        
        availableUpgrades.Add(new TomeUpgrade {
            upgradeName = "Meteor fall speed +5m/s",
            upgradeType = UpgradeType.MeteorFallSpeed,
            floatValue = 5f,
            affectedTome = TomeType.MeteoriteTome
        });
    }

    public void AddExperience(float amount)
    {
        currentExperience += amount;

        float experienceToNextLevel = GetExperienceForLevel(currentLevel + 1);

        if (currentExperience >= experienceToNextLevel)
        {
            LevelUp();
        }

        UpdateExperienceUI();
    }

    private void LevelUp()
    {
        currentLevel++;

        List<TomeUpgrade> upgradeOptions = GetRandomUpgradeOptions(3);
        upgradeSelectionUI.ShowUpgradeSelection(upgradeOptions, this);
        PlayLevelUpEffects();
    }

    private void PlayLevelUpEffects()
    {
        //probably just add some sound?
    }

    private List<TomeUpgrade> GetRandomUpgradeOptions(int count)
    {
        List<TomeUpgrade> options = new List<TomeUpgrade>();
        List<TomeUpgrade> tempList = new List<TomeUpgrade>(availableUpgrades);

        count = Mathf.Min(count, tempList.Count);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, tempList.Count);
            options.Add(tempList[randomIndex]);
            tempList.RemoveAt(randomIndex);
        }

        return options;
    }

    public void ApplyUpgrade(TomeUpgrade upgrade)
    {
        if (tomeController == null)
        {
            Debug.LogError("TomeController is null!");
            return;
        }
        switch (upgrade.upgradeType)
        {
            case UpgradeType.PlayerHealth:
                playerStats.IncreaseHealth(upgrade.floatValue);
                break;
            case UpgradeType.PlayerArmour:
                playerStats.IncreaseArmour(upgrade.floatValue);
                break;
            case UpgradeType.GlobalDamage:
                tomeController.ApplyGlobalDamageBonus(upgrade.floatValue);
                break;
            case UpgradeType.GlobalAttackSpeed:
                tomeController.ApplyGlobalAttackSpeed(upgrade.floatValue);
                break;
            case UpgradeType.LifeRegenAdd:
                playerStats.IncreaseLifeRegenerationAmount(upgrade.floatValue);
                break;
            case UpgradeType.LifeRegenRate:
                playerStats.IncreaseLifeRegenerationSpeed(upgrade.floatValue);
                break;
            case UpgradeType.ProjectileSize:
            case UpgradeType.AreaOfEffect:
            case UpgradeType.ProjectileCount:
            case UpgradeType.ProjectileSpeed:
            case UpgradeType.DamageOverTime:
            case UpgradeType.MeteorFallSpeed:
                tomeController.ApplyTomeUpgrade(upgrade.affectedTome, upgrade.upgradeType, upgrade.floatValue);
                break;
        }

        ShowUpgradeMessage(upgrade.upgradeName);
    }

    private void ShowUpgradeMessage(string message)
    {
        upgradeText.text = $"{message} (Level {currentLevel})";
        StartCoroutine(DisplayMessageCoroutine());
    }

    private IEnumerator DisplayMessageCoroutine()
    {
        upgradeText.gameObject.SetActive(true);
        yield return new WaitForSeconds(messageDuration);
        upgradeText.gameObject.SetActive(false);
    }

    public float GetExperienceForLevel(int level)
    {
        if (level <= 1) return 0f;

        return baseExperiencePerLevel * (1 - Mathf.Pow(experienceGrowthFactor, level - 1)) / (1 - experienceGrowthFactor);
    }

    public float GetLevelProgress()
    {
        float currentLevelXP = GetExperienceForLevel(currentLevel);
        float nextLevelXP = GetExperienceForLevel(currentLevel + 1);
        float levelExperienceRange = nextLevelXP - currentLevelXP;

        if (levelExperienceRange <= 0)
            return 0;

        return (currentExperience - currentLevelXP) / levelExperienceRange;
    }

    private void UpdateExperienceUI()
    {
        if (experienceText != null)
        {
            float nextLevelXP = GetExperienceForLevel(currentLevel + 1);
            float currentLevelXP = GetExperienceForLevel(currentLevel);
            experienceText.text = $"Level {currentLevel} | {Mathf.Floor(currentExperience - currentLevelXP)}/{Mathf.Floor(nextLevelXP - currentLevelXP)} XP";
        }

        if (experienceBar != null)
        {
            float progress = GetLevelProgress();

            experienceBar.anchorMin = new Vector2(0, 0);
            experienceBar.anchorMax = new Vector2(progress, 1);
            experienceBar.offsetMin = Vector2.zero;
            experienceBar.offsetMax = Vector2.zero;
        }
    }
}