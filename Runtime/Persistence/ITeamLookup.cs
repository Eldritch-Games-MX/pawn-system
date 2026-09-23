using EldritchGames.PawnSystem.Identity;

namespace EldritchGames.PawnSystem.Persistence
{
    /// <summary>
    /// Turns a saved team id back into the <see cref="TeamDefinition"/> asset it came from.
    /// </summary>
    /// <remarks>
    /// Save data stores ids, not object references, so restoring needs a way back to the assets.
    /// How a project finds them — a serialized catalogue, Addressables, a <c>Resources</c> folder —
    /// is the project's business, so this package only asks the question.
    /// <code>
    /// public sealed class CatalogueTeamLookup : ITeamLookup
    /// {
    ///     [SerializeField] private List&lt;TeamDefinition&gt; teams;
    ///
    ///     public TeamDefinition FindTeam(string id) => teams.Find(t => t.Id == id);
    /// }
    /// </code>
    /// </remarks>
    public interface ITeamLookup
    {
        /// <summary>Finds the team with this id.</summary>
        /// <param name="id">The saved <see cref="TeamDefinition.Id"/>.</param>
        /// <returns>The matching asset, or <c>null</c> when the id is unknown — restoring then leaves the pawn's team unchanged rather than clearing it.</returns>
        TeamDefinition FindTeam(string id);
    }
}
