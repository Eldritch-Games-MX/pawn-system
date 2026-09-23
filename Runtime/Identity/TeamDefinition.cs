using System.Collections.Generic;
using UnityEngine;

namespace EldritchGames.PawnSystem.Identity
{
    /// <summary>
    /// An authored team, faction or side. Pawns on the same team are allies by default; the
    /// <see cref="HostileTeams"/> and <see cref="FriendlyTeams"/> lists describe everything else.
    /// </summary>
    /// <remarks>
    /// One asset per side — <c>Players</c>, <c>Monsters</c>, <c>TownGuard</c>. The relationship
    /// lists are read by <see cref="DefaultTeamResolver"/>; they are authored per team rather than
    /// in one global matrix so a project can add a faction without editing a central asset.
    /// <para>
    /// Relationships are not forced to be symmetric — the guards can be hostile to the thieves
    /// while the thieves stay neutral. When you want symmetry, list the team on both assets.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Eldritch Games/Pawn System/Team Definition", fileName = "NewTeamDefinition")]
    public sealed class TeamDefinition : ScriptableObject
    {
        [Tooltip("Stable identifier used by save data to find this team again. Never shown to players.")]
        [SerializeField] private string id = string.Empty;

        [Tooltip("Human-readable name for UI and debugging.")]
        [SerializeField] private string displayName = string.Empty;

        [Tooltip("Colour for UI, debug gizmos and minimap markers. Never read by the runtime.")]
        [SerializeField] private Color color = Color.white;

        [Tooltip("Teams this team treats as enemies.")]
        [SerializeField] private List<TeamDefinition> hostileTeams = new List<TeamDefinition>();

        [Tooltip("Teams this team treats as allies, in addition to itself.")]
        [SerializeField] private List<TeamDefinition> friendlyTeams = new List<TeamDefinition>();

        /// <summary>Stable identifier used by save data to resolve this asset. Never shown to players.</summary>
        public string Id => id;

        /// <summary>Human-readable name for UI and debugging.</summary>
        public string DisplayName => displayName;

        /// <summary>Presentation colour. Authoring metadata — no runtime code reads it.</summary>
        public Color Color => color;

        /// <summary>Teams this team treats as <see cref="PawnRelationship.Hostile"/>.</summary>
        public IReadOnlyList<TeamDefinition> HostileTeams => hostileTeams;

        /// <summary>Teams this team treats as <see cref="PawnRelationship.Friendly"/>, in addition to itself.</summary>
        public IReadOnlyList<TeamDefinition> FriendlyTeams => friendlyTeams;
    }
}
