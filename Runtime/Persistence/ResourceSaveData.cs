using System;

namespace EldritchGames.PawnSystem.Persistence
{
    /// <summary>One registered secondary resource's saved state.</summary>
    [Serializable]
    public sealed class ResourceSaveData
    {
        /// <summary>The <see cref="Vitals.ResourceDefinition.Id"/> of the resource.</summary>
        public string resourceId;

        /// <summary>The current value at the moment of capture.</summary>
        public float current;

        /// <summary>The maximum value at the moment of capture.</summary>
        public float max;
    }
}
