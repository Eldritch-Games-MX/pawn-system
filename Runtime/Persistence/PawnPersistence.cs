using System;
using System.Collections.Generic;
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
                schemaVersion = PawnSaveData.CurrentSchemaVersion,
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

            var resources = new List<ResourceSaveData>();
            foreach (ResourceDefinition resource in pawn.Resources.RegisteredResources)
            {
                resources.Add(new ResourceSaveData
                {
                    resourceId = resource.Id,
                    current = pawn.Resources.GetCurrent(resource),
                    max = pawn.Resources.GetMax(resource)
                });
            }
            data.resources = resources.ToArray();

            return data;
        }

        /// <summary>
        /// Puts <paramref name="pawn"/> back into the state <paramref name="data"/> describes:
        /// place, spawn or despawn, restore vitals, team and tags.
        /// </summary>
        /// <param name="pawn">The pawn to restore into.</param>
        /// <param name="data">The record to restore from.</param>
        /// <param name="teamLookup">Resolves the saved team id back to an asset. <c>null</c> leaves the pawn's current team alone.</param>
        /// <param name="resourceLookup">Resolves saved resource ids back to assets. <c>null</c> skips restoring secondary resources entirely.</param>
        /// <exception cref="ArgumentNullException"><paramref name="pawn"/> or <paramref name="data"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Unknown team ids, tags and resource ids are restored as far as they can be rather than
        /// throwing — a renamed team or resource leaves that one thing at its default instead of
        /// failing the whole load. A pawn restored into <see cref="PawnState.Incapacitated"/>
        /// raises <see cref="Pawn.Incapacitated"/> during the load, the same way a pawn restored
        /// into <see cref="PawnState.Dead"/> raises <see cref="Pawn.Died"/>.
        /// </remarks>
        public static void RestoreState(this Pawn pawn, PawnSaveData data, ITeamLookup teamLookup = null, IResourceLookup resourceLookup = null)
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
                case PawnState.Incapacitated:
                    RestoreIntoWorld(pawn, data, teamLookup, resourceLookup, state);
                    return;

                default:
                    return;
            }
        }

        private static void RestoreIntoWorld(Pawn pawn, PawnSaveData data, ITeamLookup teamLookup, IResourceLookup resourceLookup, PawnState state)
        {
            if (pawn.State == PawnState.Dead || pawn.State == PawnState.Incapacitated) pawn.TryRevive(ReviveInfo.Full);
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
            RestoreResources(pawn, data, resourceLookup);

            if (state == PawnState.Dead)
            {
                pawn.Kill();
                return;
            }

            if (state == PawnState.Incapacitated)
            {
                float toZero = -pawn.Vitals.Current;
                if (toZero != 0f) pawn.Vitals.ApplyDelta(toZero);
                pawn.Incapacitate(default);
                return;
            }

            float delta = data.vitalCurrent - pawn.Vitals.Current;
            if (delta != 0f) pawn.Vitals.ApplyDelta(delta);
        }

        private static void RestoreResources(Pawn pawn, PawnSaveData data, IResourceLookup resourceLookup)
        {
            if (resourceLookup == null || data.resources == null) return;

            for (int i = 0; i < data.resources.Length; i++)
            {
                ResourceSaveData saved = data.resources[i];
                if (saved == null) continue;

                ResourceDefinition resource = resourceLookup.FindResource(saved.resourceId);
                if (resource == null) continue;

                pawn.Resources.Restore(resource, saved.max, saved.current);
            }
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
