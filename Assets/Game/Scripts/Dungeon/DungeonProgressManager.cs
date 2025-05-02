using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

public class DungeonProgressManager : MonoBehaviour
{
    private static DungeonProgressManager _instance; //local
    public static DungeonProgressManager Instance { get { return _instance; } } //for other scripts

    [SerializeField] private int dungeonsCompletedCount = 0;
    [SerializeField] private int dungeonsNeededForBoss = 3;
    
    [SerializeField] public bool isBossDungeonAvailable = false;
    [SerializeField] private bool isBossDungeonCompleted = false;
    
    public event Action OnBossDungeonAvailable;
    public event Action OnBossDungeonCompleted;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public void CompleteDungeon(DungeonOption completedDungeon = null)
    {
        if (isBossDungeonAvailable && !isBossDungeonCompleted) //if completing boss dungeon
        {
            StartCoroutine(FadeToWinMenu());
            return;
        }
        if (completedDungeon != null) //regular dungeons provide upgrades
        {
            ApplyDungeonUpgrade(completedDungeon);
        }

        dungeonsCompletedCount++;

        if (dungeonsCompletedCount >= dungeonsNeededForBoss && !isBossDungeonAvailable)
        {
            isBossDungeonAvailable = true;
            OnBossDungeonAvailable?.Invoke();
        }
    }

    public void SetBossDungeonCompleted() //public method called when the boss dungeon is completed
    {
        if (!isBossDungeonCompleted)
        {
            isBossDungeonCompleted = true;
            OnBossDungeonCompleted?.Invoke();
        }
    }

    public void ApplyDungeonUpgrade(DungeonOption dungeon)
    {
        if (dungeon == null) return;
        
        PlayerLeveling playerLeveling = FindObjectOfType<PlayerLeveling>();
        if (playerLeveling == null) return;

        var upgrade = new PlayerLeveling.TomeUpgrade
        {
            upgradeName = dungeon.upgradeName,
            upgradeType = dungeon.upgradeType,
            floatValue = dungeon.upgradeValue,
            affectedTome = TomeType.None //dungeons provide global upgrades, not tome specific
        };
        
        playerLeveling.ApplyUpgrade(upgrade);
    }

    private IEnumerator FadeToWinMenu()
    {
        isBossDungeonCompleted = true;
        OnBossDungeonCompleted?.Invoke();
        
        ScreenFade fader = FindObjectOfType<ScreenFade>();
        if (fader != null)
            yield return StartCoroutine(fader.FadeOut());

        SceneManager.LoadScene("WinMenuScene");
    }

    public bool IsBossDungeonAvailable() => isBossDungeonAvailable; //bool checks and getters for other scripts
    public bool IsBossDungeonCompleted() => isBossDungeonCompleted;
    public int GetDungeonsCompletedCount() => dungeonsCompletedCount;
    public int GetDungeonsNeededForBoss() => dungeonsNeededForBoss;
}