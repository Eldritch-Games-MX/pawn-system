using System;
using UnityEngine;

namespace EldritchGames.PawnSystem.Persistence
{
    /// <summary>
    /// A pawn's state flattened for saving: identity, lifecycle state, vitals, team, tags and
    /// where it stood.
    /// </summary>
    /// <remarks>
    /// Plain serializable fields so Unity's <c>JsonUtility</c>, a binary writer or any other
    /// serializer can take it straight. Assets are referenced by their string ids rather than by
    /// object reference, so a save survives asset reimports and moves.
    /// <code>
    /// PawnSaveData data = pawn.CaptureState();
    /// string json = JsonUtility.ToJson(data);
    /// // ... later ...
    /// pawn.RestoreState(JsonUtility.FromJson&lt;PawnSaveData&gt;(json), teamLookup);
    /// </code>
    /// Public lowercase fields match the save-data convention used by the Eldritch Ability System.
    /// </remarks>
    [Serializable]
    public sealed class PawnSaveData
    {
        /// <summary>
        /// The shape of this record. Bump <see cref="CurrentSchemaVersion"/> and branch on this
        /// field in <see cref="PawnPersistence.RestoreState"/> when a future field addition or
        /// removal needs a migration — records with no version at all (from before this field
        /// existed) deserialize as <c>0</c>, which is deliberately not a valid current version.
        /// </summary>
        public int schemaVersion;

        /// <summary>The <see cref="Identity.PawnDefinition.Id"/> of the pawn's definition, used to match this record to a pawn.</summary>
        public string pawnId;

        /// <summary>The pawn's <see cref="PawnState"/> as an integer.</summary>
        public int state;

        /// <summary>The pawn's vitals at the moment of capture.</summary>
        public float vitalCurrent;

        /// <summary>The pawn's maximum vitals at the moment of capture, which buffs may have changed.</summary>
        public float vitalMax;

        /// <summary>The <see cref="Identity.TeamDefinition.Id"/> of the pawn's team, or empty when it had none.</summary>
        public string teamId;

        /// <summary>The pawn's runtime tags in dotted string form.</summary>
        public string[] tags = Array.Empty<string>();

        /// <summary>World-space position at the moment of capture.</summary>
        public Vector3 position;

        /// <summary>World-space rotation at the moment of capture.</summary>
        public Quaternion rotation;

        /// <summary>Every registered secondary resource pool at the moment of capture.</summary>
        public ResourceSaveData[] resources = Array.Empty<ResourceSaveData>();

        /// <summary>The current schema shape. Bump this when <see cref="PawnSaveData"/>'s fields change in a way older saves cannot deserialize into directly.</summary>
        public const int CurrentSchemaVersion = 1;
    }
}
