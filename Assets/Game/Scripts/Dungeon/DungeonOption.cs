using UnityEngine;

[CreateAssetMenu(fileName = "New Dungeon Option", menuName = "Dungeon/Dungeon Option")]
public class DungeonOption : ScriptableObject
{
    public string dungeonName;
    public string upgradeName;
    public UpgradeType upgradeType;
    public float upgradeValue;
    public int roomCountMin;
    public int roomCountMax;
}