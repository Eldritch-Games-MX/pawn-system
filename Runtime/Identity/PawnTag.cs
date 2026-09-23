using System;
using UnityEngine;

namespace EldritchGames.PawnSystem.Identity
{
    /// <summary>
    /// A hierarchical, dotted string label attached to a <see cref="Pawn"/> —
    /// <c>"Class.Rogue"</c>, <c>"Status.Stunned"</c>, <c>"Faction.Undead"</c>.
    /// </summary>
    /// <remarks>
    /// Tags are how a genre-agnostic package lets a game classify pawns without the package
    /// knowing any of the categories. Matching is hierarchical: a pawn tagged
    /// <c>"Status.Stunned"</c> answers <c>true</c> to a query for <c>"Status"</c>, so systems
    /// can ask broad questions ("is anything affecting this pawn?") or narrow ones.
    /// <code>
    /// pawn.Tags.Add("Status.Stunned");
    /// pawn.Tags.Has("Status");          // true  — parent match
    /// pawn.Tags.Has("Status.Stunned");  // true  — exact match
    /// pawn.Tags.Has("Status.Burning");  // false — sibling, not a match
    /// </code>
    /// <para>
    /// This mirrors <c>GameplayTag</c> in <c>EldritchGames.AbilitySystem</c> — same dotted
    /// convention, same semantics — but is declared here so this package keeps zero package
    /// dependencies. Projects that use both convert through the string form.
    /// </para>
    /// </remarks>
    [Serializable]
    public struct PawnTag : IEquatable<PawnTag>
    {
        [SerializeField] private string value;

        /// <summary>Creates a tag from its dotted string form. Surrounding whitespace is trimmed.</summary>
        /// <param name="value">The dotted tag text, for example <c>"Status.Stunned"</c>.</param>
        public PawnTag(string value)
        {
            this.value = Normalize(value);
        }

        /// <summary>The dotted text of this tag, or an empty string when the tag is not set.</summary>
        public string Value => value ?? string.Empty;

        /// <summary><c>true</c> when this tag carries any text; a default-constructed tag is not valid.</summary>
        public bool IsValid => !string.IsNullOrEmpty(Value);

        /// <summary>
        /// Returns <c>true</c> when this tag is <paramref name="parent"/> itself or sits beneath it
        /// in the dotted hierarchy — <c>"Status.Stunned"</c> is a child of <c>"Status"</c>.
        /// </summary>
        /// <param name="parent">The ancestor tag to test against. An invalid tag never matches.</param>
        /// <returns><c>true</c> when this tag equals or descends from <paramref name="parent"/>.</returns>
        public bool IsChildOf(PawnTag parent)
        {
            if (!parent.IsValid) return false;
            if (string.Equals(Value, parent.Value, StringComparison.Ordinal)) return true;
            return Value.StartsWith(parent.Value + ".", StringComparison.Ordinal);
        }

        /// <summary>Ordinal equality on the dotted text.</summary>
        /// <param name="other">The tag to compare with.</param>
        public bool Equals(PawnTag other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is PawnTag other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>Returns the dotted text of this tag.</summary>
        public override string ToString() => Value;

        /// <summary>Allows a string literal to be used wherever a tag is expected.</summary>
        /// <param name="value">The dotted tag text.</param>
        public static implicit operator PawnTag(string value) => new PawnTag(value);

        /// <summary>Ordinal equality on the dotted text.</summary>
        public static bool operator ==(PawnTag a, PawnTag b) => a.Equals(b);

        /// <summary>Ordinal inequality on the dotted text.</summary>
        public static bool operator !=(PawnTag a, PawnTag b) => !a.Equals(b);

        private static string Normalize(string raw) => string.IsNullOrWhiteSpace(raw) ? string.Empty : raw.Trim();
    }
}
