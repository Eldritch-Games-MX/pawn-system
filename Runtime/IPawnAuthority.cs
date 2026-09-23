namespace EldritchGames.PawnSystem
{
    /// <summary>
    /// Marks who is authoritative over a <see cref="Pawn"/> in a networked game — the server, the
    /// owning client, whatever the netcode layer decides.
    /// </summary>
    /// <remarks>
    /// This is a data-only marker, not enforcement. Nothing in this package reads
    /// <see cref="HasAuthority"/> before calling <see cref="Pawn.ApplyDamage"/>,
    /// <see cref="Pawn.TryPossess"/>, or anything else — the package stays engine- and
    /// netcode-agnostic by never deciding that policy itself. A networked adapter checks it
    /// before forwarding a call locally, or before trusting one that arrived over the wire.
    /// <code>
    /// // a thin adapter over whatever netcode the game uses
    /// public sealed class NetcodePawnAuthority : IPawnAuthority
    /// {
    ///     private readonly NetworkObject networkObject;
    ///     public NetcodePawnAuthority(NetworkObject networkObject) => this.networkObject = networkObject;
    ///     public bool HasAuthority =&gt; networkObject.IsServer;
    /// }
    ///
    /// pawn.Authority = new NetcodePawnAuthority(networkObject);
    ///
    /// // game code checks before mutating
    /// if (pawn.HasAuthority) pawn.ApplyDamage(damage);
    /// </code>
    /// <see cref="Pawn.Authority"/> is <c>null</c> by default, and <see cref="Pawn.HasAuthority"/>
    /// answers <c>true</c> when it is — a single-player or non-networked game never has to know
    /// this interface exists.
    /// </remarks>
    public interface IPawnAuthority
    {
        /// <summary>Whether the current caller is allowed to mutate the pawn this authority is attached to.</summary>
        bool HasAuthority { get; }
    }
}
