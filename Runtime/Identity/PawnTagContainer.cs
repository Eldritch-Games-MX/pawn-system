using System;
using System.Collections;
using System.Collections.Generic;

namespace EldritchGames.PawnSystem.Identity
{
    /// <summary>
    /// The set of <see cref="PawnTag"/>s currently on a <see cref="Pawn"/>. Queries are
    /// hierarchical, so a container holding <c>"Status.Stunned"</c> answers <c>true</c> to
    /// <c>Has("Status")</c>.
    /// </summary>
    /// <remarks>
    /// Runtime state, not authored data: a pawn starts with the tags listed on its
    /// <see cref="PawnDefinition"/> and the game adds and removes more as the pawn plays.
    /// <code>
    /// pawn.Tags.Add("Status.Stunned");
    /// if (pawn.Tags.Has("Status")) { /* something is affecting this pawn */ }
    /// pawn.Tags.Remove("Status.Stunned");
    /// </code>
    /// <see cref="TagAdded"/> and <see cref="TagRemoved"/> fire only on an actual change —
    /// adding a tag that is already present is a no-op and raises nothing.
    /// </remarks>
    public sealed class PawnTagContainer : IEnumerable<PawnTag>
    {
        private readonly List<PawnTag> tags = new List<PawnTag>();

        /// <summary>Raised after a tag that was not present is added.</summary>
        public event Action<PawnTag> TagAdded;

        /// <summary>Raised after a tag that was present is removed.</summary>
        public event Action<PawnTag> TagRemoved;

        /// <summary>How many tags the container currently holds.</summary>
        public int Count => tags.Count;

        /// <summary>Adds <paramref name="tag"/>. No-ops for an invalid tag or one already present.</summary>
        /// <param name="tag">The tag to add.</param>
        /// <returns><c>true</c> when the container changed.</returns>
        public bool Add(PawnTag tag)
        {
            if (!tag.IsValid || tags.Contains(tag)) return false;
            tags.Add(tag);
            TagAdded?.Invoke(tag);
            return true;
        }

        /// <summary>
        /// Removes <paramref name="tag"/> by exact match. Removing <c>"Status"</c> does
        /// <b>not</b> remove <c>"Status.Stunned"</c> — hierarchy applies to queries, not removal.
        /// </summary>
        /// <param name="tag">The tag to remove.</param>
        /// <returns><c>true</c> when the container changed.</returns>
        public bool Remove(PawnTag tag)
        {
            if (!tags.Remove(tag)) return false;
            TagRemoved?.Invoke(tag);
            return true;
        }

        /// <summary>Returns <c>true</c> when any held tag is <paramref name="tag"/> or descends from it.</summary>
        /// <param name="tag">The tag, or parent tag, to look for.</param>
        public bool Has(PawnTag tag)
        {
            if (!tag.IsValid) return false;
            for (int i = 0; i < tags.Count; i++)
                if (tags[i].IsChildOf(tag))
                    return true;
            return false;
        }

        /// <summary>Returns <c>true</c> when <see cref="Has"/> holds for at least one entry of <paramref name="query"/>.</summary>
        /// <param name="query">The tags to test. A <c>null</c> or empty query returns <c>false</c>.</param>
        public bool HasAny(IReadOnlyList<PawnTag> query)
        {
            if (query == null) return false;
            for (int i = 0; i < query.Count; i++)
                if (Has(query[i]))
                    return true;
            return false;
        }

        /// <summary>Returns <c>true</c> when <see cref="Has"/> holds for every entry of <paramref name="query"/>.</summary>
        /// <param name="query">The tags to test. A <c>null</c> or empty query returns <c>true</c> — nothing is required.</param>
        public bool HasAll(IReadOnlyList<PawnTag> query)
        {
            if (query == null) return true;
            for (int i = 0; i < query.Count; i++)
                if (!Has(query[i]))
                    return false;
            return true;
        }

        /// <summary>Removes every tag, raising <see cref="TagRemoved"/> once per tag.</summary>
        public void Clear()
        {
            for (int i = tags.Count - 1; i >= 0; i--)
            {
                PawnTag removed = tags[i];
                tags.RemoveAt(i);
                TagRemoved?.Invoke(removed);
            }
        }

        /// <summary>Enumerates the tags currently held, in insertion order.</summary>
        public List<PawnTag>.Enumerator GetEnumerator() => tags.GetEnumerator();

        IEnumerator<PawnTag> IEnumerable<PawnTag>.GetEnumerator() => tags.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => tags.GetEnumerator();
    }
}
