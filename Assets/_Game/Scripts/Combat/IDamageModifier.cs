namespace AICompanionRoguelike.Combat
{
    public interface IDamageModifier
    {
        DamageInfo ModifyIncomingDamage(HealthComponent target, DamageInfo damageInfo);
    }
}
