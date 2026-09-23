using UnityEngine;

namespace EldritchGames.PawnSystem
{
    /// <summary>
    /// Marks a <c>[SerializeReference]</c> field or list so the inspector draws a type picker
    /// listing every implementation of the field's interface.
    /// </summary>
    /// <remarks>
    /// Used on <see cref="Identity.PawnDefinition.DamageModifiers"/>; use it on your own polymorphic
    /// serialized fields too.
    /// <code>
    /// [SerializeReference, SerializeReferenceDropdown]
    /// private List&lt;IDamageModifier&gt; damageModifiers = new List&lt;IDamageModifier&gt;();
    /// </code>
    /// The picker is drawn by <c>SerializeReferencePolymorphicDrawer</c> in the Editor assembly
    /// and lists non-abstract types with a public parameterless constructor, annotated with their
    /// <see cref="PawnDocAttribute"/> summary when they have one.
    /// <para>
    /// If you rename or move a class referenced this way, add
    /// <c>[UnityEngine.Scripting.APIUpdating.MovedFrom]</c> to it — otherwise existing assets
    /// silently lose that entry. Run <b>Eldritch Games &gt; Pawn System &gt; Validate Pawn
    /// Definitions</b> after any such rename.
    /// </para>
    /// </remarks>
    public sealed class SerializeReferenceDropdownAttribute : PropertyAttribute
    {
    }
}
