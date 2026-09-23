namespace EldritchGames.PawnSystem.Vitals
{
    /// <summary>
    /// How a dead <see cref="Pawn"/> comes back: how much of its vitals are restored, who brought
    /// it back, and how long it is protected afterwards.
    /// </summary>
    /// <remarks>
    /// Revival is always explicit. Healing a dead pawn does nothing — a corpse is brought back by
    /// <see cref="Pawn.TryRevive"/> and no other path, so a stray heal can never silently
    /// resurrect something.
    /// <code>
    /// pawn.TryRevive(ReviveInfo.Full);                       // full vitals, definition's grace period
    /// pawn.TryRevive(new ReviveInfo(0.25f, instigator: medic)); // a quarter of max
    /// </code>
    /// </remarks>
    public readonly struct ReviveInfo
    {
        /// <summary>
        /// The fraction of maximum vitals to restore, in <c>(0, 1]</c>. A value of zero or less is
        /// treated as a full revive, so <c>default(ReviveInfo)</c> means "come back whole".
        /// </summary>
        public float VitalFraction { get; }

        /// <summary>The pawn responsible for the revival, or <c>null</c> for a scripted or self revive.</summary>
        public Pawn Instigator { get; }

        /// <summary>
        /// Seconds of invulnerability granted after the revival. Negative means "use the pawn's
        /// <see cref="Identity.PawnDefinition.SpawnInvulnerability"/>".
        /// </summary>
        public float InvulnerabilityDuration { get; }

        /// <summary>Creates a revival record.</summary>
        /// <param name="vitalFraction">Fraction of maximum vitals to restore; zero or less means full.</param>
        /// <param name="instigator">The pawn responsible, or <c>null</c>.</param>
        /// <param name="invulnerabilityDuration">Seconds of grace; negative uses the definition's value.</param>
        public ReviveInfo(float vitalFraction = 1f, Pawn instigator = null, float invulnerabilityDuration = -1f)
        {
            VitalFraction = vitalFraction;
            Instigator = instigator;
            InvulnerabilityDuration = invulnerabilityDuration;
        }

        /// <summary>A full revival with the definition's default grace period and no instigator.</summary>
        public static ReviveInfo Full => new ReviveInfo(1f);
    }
}
