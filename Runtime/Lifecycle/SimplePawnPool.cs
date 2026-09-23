using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.PawnSystem.Lifecycle
{
    /// <summary>
    /// The built-in <see cref="IPawnPool"/>: one stack of idle instances per prefab, instantiating
    /// only when the stack for that prefab is empty.
    /// </summary>
    /// <remarks>
    /// Deliberately small — no pre-warming, no maximum size, no trimming. It exists so a game can
    /// stop instantiating on every wave with one line, and as the worked example for a pool with
    /// real policy.
    /// <code>
    /// spawner.Pool = new SimplePawnPool(poolRoot);
    /// </code>
    /// Returned instances are parented under <c>poolRoot</c> when one is given, which keeps the
    /// hierarchy readable while they sit idle.
    /// </remarks>
    public sealed class SimplePawnPool : IPawnPool
    {
        private readonly Dictionary<Pawn, Stack<Pawn>> idle = new Dictionary<Pawn, Stack<Pawn>>();
        private readonly Dictionary<Pawn, Pawn> origin = new Dictionary<Pawn, Pawn>();
        private readonly Transform root;

        /// <summary>Creates a pool.</summary>
        /// <param name="root">Optional parent for idle instances. <c>null</c> leaves them at the scene root.</param>
        public SimplePawnPool(Transform root = null)
        {
            this.root = root;
        }

        /// <summary>How many idle instances the pool is holding across every prefab.</summary>
        public int IdleCount
        {
            get
            {
                int total = 0;
                foreach (KeyValuePair<Pawn, Stack<Pawn>> entry in idle) total += entry.Value.Count;
                return total;
            }
        }

        /// <inheritdoc/>
        /// <remarks>Reuses an idle instance of the same prefab when one exists, otherwise instantiates a new one.</remarks>
        public Pawn Rent(Pawn prefab)
        {
            if (prefab == null) return null;

            if (idle.TryGetValue(prefab, out Stack<Pawn> stack) && stack.Count > 0)
            {
                Pawn reused = stack.Pop();
                if (reused != null) return reused;
            }

            Pawn created = Object.Instantiate(prefab);
            origin[created] = prefab;
            return created;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Instances the pool did not create are destroyed rather than kept, so a pool never hands
        /// out something it cannot match to a prefab.
        /// </remarks>
        public void Return(Pawn pawn)
        {
            if (pawn == null) return;

            if (!origin.TryGetValue(pawn, out Pawn prefab))
            {
                // Destroy is a play-mode-only call; edit-mode tooling and EditMode tests need the immediate form.
                if (Application.isPlaying) Object.Destroy(pawn.gameObject);
                else Object.DestroyImmediate(pawn.gameObject);
                return;
            }

            if (!idle.TryGetValue(prefab, out Stack<Pawn> stack))
            {
                stack = new Stack<Pawn>();
                idle[prefab] = stack;
            }

            if (root != null) pawn.transform.SetParent(root, false);
            stack.Push(pawn);
        }
    }
}
