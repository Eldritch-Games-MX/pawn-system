using EldritchGames.PawnSystem.Vitals;

namespace EldritchGames.PawnSystem.Persistence
{
    /// <summary>
    /// Turns a saved resource id back into the <see cref="ResourceDefinition"/> asset it came from.
    /// </summary>
    /// <remarks>
    /// Save data stores ids, not object references, so restoring needs a way back to the assets.
    /// How a project finds them — a serialized catalogue, Addressables, a <c>Resources</c> folder —
    /// is the project's business, so this package only asks the question. Mirrors
    /// <see cref="ITeamLookup"/> exactly.
    /// <code>
    /// public sealed class CatalogueResourceLookup : IResourceLookup
    /// {
    ///     [SerializeField] private List&lt;ResourceDefinition&gt; resources;
    ///
    ///     public ResourceDefinition FindResource(string id) => resources.Find(r => r.Id == id);
    /// }
    /// </code>
    /// </remarks>
    public interface IResourceLookup
    {
        /// <summary>Finds the resource with this id.</summary>
        /// <param name="id">The saved <see cref="ResourceDefinition.Id"/>.</param>
        /// <returns>The matching asset, or <c>null</c> when the id is unknown — that saved pool is then skipped rather than failing the whole restore.</returns>
        ResourceDefinition FindResource(string id);
    }
}
