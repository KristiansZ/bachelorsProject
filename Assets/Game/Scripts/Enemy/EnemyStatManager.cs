using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Roguelike/EnemyStats")]
public class EnemyStats : ScriptableObject
{
    [Header("Base Stats")]
    public float maxHealth = 30f;
    public float moveSpeed = 3.5f;
    public float attackSpeed = 1f;
    public float damage = 10f;
    public float attackRange = 1.8f;
    public int xpReward = 10;

    [Header("Scaling")]
    public float healthScaling = 1.1f; // 10% increase per level so later dungeon dont feel too easy
    public float damageScaling = 1.05f;

    public EnemyStats GetScaledVersion(int level)
    {
        EnemyStats clone = ScriptableObject.CreateInstance<EnemyStats>();
        
        clone.maxHealth = maxHealth * Mathf.Pow(healthScaling, level);
        clone.moveSpeed = moveSpeed;
        clone.attackSpeed = attackSpeed;
        clone.damage = damage * Mathf.Pow(damageScaling, level);
        clone.attackRange = attackRange;
        clone.xpReward = xpReward + (level * 2);
        
        return clone;
    }
}
