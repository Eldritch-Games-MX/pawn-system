using System;

namespace EldritchGames.PawnSystem
{
    /// <summary>
    /// An optional one-line summary shown beside a type in the inspector's
    /// <c>[SerializeReference]</c> picker — purely an authoring aid, never read by runtime code.
    /// </summary>
    /// <remarks>
    /// Attach it to your own <see cref="Vitals.IDamageModifier"/> implementations so designers see
    /// what each entry does without opening the script.
    /// <code>
    /// [Serializable]
    /// [PawnDoc("Halves damage while the pawn is blocking.")]
    /// public sealed class BlockModifier : IDamageModifier { /* ... */ }
    /// </code>
    /// This mirrors <c>AbilityDocAttribute</c> in the Eldritch Ability System and is declared here
    /// so this package keeps zero package dependencies.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class PawnDocAttribute : Attribute
    {
        /// <summary>Creates the attribute.</summary>
        /// <param name="summary">One line describing what the type does, shown in the picker.</param>
        public PawnDocAttribute(string summary)
        {
            Summary = summary ?? string.Empty;
        }

        /// <summary>The one-line summary shown in the picker and inline once assigned.</summary>
        public string Summary { get; }
    }
}
