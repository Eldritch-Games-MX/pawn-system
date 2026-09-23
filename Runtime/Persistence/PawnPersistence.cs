using System;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.Vitals;
using UnityEngine;

namespace EldritchGames.PawnSystem.Persistence
{
    /// <summary>
    /// Saving and loading for pawns, as opt-in extension methods — a game that never saves pays
    /// nothing for this.
    /// </summary>
    /// <remarks>
    /// <code>
    /// PawnSaveData data = pawn.CaptureState();
    /// // ... write it out, read it back ...
    /// pawn.RestoreState(data, teamLookup);
    /// </code>
    /// Restoring drives the pawn through its normal lifecycle methods rather than writing state
    /// behind their backs, so listeners see a coherent pawn. That does mean a pawn restored into
    /// <see cref="PawnState.Dead"/> raises <see cref="Pawn.Died"/> during the load — suppress
    /// death feedback while restoring, or restore before wiring listeners up.
    /// </remarks>
    public static class PawnPersistence
    {
        /// <summary>Captures everything needed to put <paramref name="pawn"/> back as it is now.</summary>
        /// <param name="pawn">The pawn to capture.</param>
        /// <returns>A fresh save record.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pawn"/> is <c>null</c>.</exception>
        public static PawnSaveData CaptureState(this Pawn pawn)
        {
            if (pawn == null) throw new ArgumentNullException(nameof(pawn));

            var data = new PawnSaveData
            {
                pawnId = pawn.Definition != null ? pawn.Definition.Id : string.Empty,
                state = (int)pawn.State,
                teamId = pawn.Team != null ? pawn.Team.Id : string.Empty,
                position = pawn.transform.position,
                rotation = pawn.transform.rotation
            };

            if (pawn.State != PawnState.Unspawned)
            {
                data.vitalCurrent = pawn.Vitals.Current;
                data.vitalMax = pawn.Vitals.Max;
            }

            int count = pawn.Tags.Count;
            var tags = new string[count];
            int index = 0;
            foreach (PawnTag tag in pawn.Tags) tags[index++] = tag.Value;
            data.tags = tags;

            return data;
        }

        /// <summary>
        /// Puts <paramref name="pawn"/> back into the state <paramref name="data"/> describes:
        /// place, spawn or despawn, restore vitals, team and tags.
        /// </summary>
        /// <param name="pawn">The pawn to restore into.</param>
        /// <param name="data">The record to restore from.</param>
        /// <param name="teamLookup">Resolves the saved team id back to an asset. <c>null</c> leaves the pawn's current team alone.</param>
        /// <exception cref="ArgumentNullException"><paramref name="pawn"/> or <paramref name="data"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Unknown team ids and tags are restored as far as they can be rather than throwing — a
        /// renamed team leaves the pawn on its default side instead of failing the whole load.
        /// </remarks>
        public static void RestoreState(this Pawn pawn, PawnSaveData data, ITeamLookup teamLookup = null)
        {
            if (pawn == null) throw new ArgumentNullException(nameof(pawn));
            if (data == null) throw new ArgumentNullException(nameof(data));

            var state = (PawnState)data.state;

            switch (state)
            {
                case PawnState.Unspawned:
                case PawnState.Despawned:
                    pawn.Despawn();
                    pawn.transform.SetPositionAndRotation(data.position, data.rotation);
                    return;

                case PawnState.Alive:
                case PawnState.Dead:
                    RestoreIntoWorld(pawn, data, teamLookup, state);
                    return;

                default:
                    return;
            }
        }

        private static void RestoreIntoWorld(Pawn pawn, PawnSaveData data, ITeamLookup teamLookup, PawnState state)
        {
            if (pawn.State == PawnState.Dead) pawn.TryRevive(ReviveInfo.Full);
            if (pawn.State != PawnState.Alive)
            {
                pawn.Despawn();
                pawn.Spawn(data.position, data.rotation);
            }
            else
            {
                pawn.transform.SetPositionAndRotation(data.position, data.rotation);
            }

            if (data.vitalMax > 0f) pawn.Vitals.SetMax(data.vitalMax);
            RestoreTeam(pawn, data, teamLookup);
            RestoreTags(pawn, data);

            if (state == PawnState.Dead)
            {
                pawn.Kill();
                return;
            }

            float delta = data.vitalCurrent - pawn.Vitals.Current;
            if (delta != 0f) pawn.Vitals.ApplyDelta(delta);
        }

        private static void RestoreTeam(Pawn pawn, PawnSaveData data, ITeamLookup teamLookup)
        {
            if (teamLookup == null || string.IsNullOrEmpty(data.teamId)) return;

            TeamDefinition team = teamLookup.FindTeam(data.teamId);
            if (team != null) pawn.Team = team;
        }

        private static void RestoreTags(Pawn pawn, PawnSaveData data)
        {
            pawn.Tags.Clear();
            if (data.tags == null) return;

            for (int i = 0; i < data.tags.Length; i++)
                pawn.Tags.Add(new PawnTag(data.tags[i]));
        }
    }
}
