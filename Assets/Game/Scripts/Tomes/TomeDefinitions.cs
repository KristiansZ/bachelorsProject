public enum TomeType
{
    None,
    FireballTome,
    MeteoriteTome,
    IceHailTome
}

public enum CastMode
{
    Directional, 
    Targeted     
}

public enum UpgradeType
{
    // Global upgrades
    PlayerHealth,
    PlayerArmour,
    PlayerMovementSpeed,
    PlayerCooldownReduction,
    GlobalDamage,
    GlobalAttackSpeed,
    LifeRegenAdd,
    LifeRegenRate,

    // Tome-specific upgrades
    ProjectileSize,
    ProjectileSpeed,
    ProjectileCount,
    AreaOfEffect,
    DamageOverTime,
    MeteorFallSpeed
}