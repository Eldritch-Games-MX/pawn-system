using EldritchGames.InteractionSystem;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.TestUtilities;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.PawnSystem.InteractionSystem.Tests.EditMode
{
    public class PawnInteractableTests
    {
        private Pawn pawn;
        private PawnInteractable interactable;

        [SetUp]
        public void SetUp()
        {
            PawnDefinition definition = new PawnDefinitionBuilder().WithId("npc").Build();
            pawn = TestPawns.Create(definition);
            // Assigned explicitly rather than relying on Awake's auto-discovery: AddComponent's
            // Awake timing is not guaranteed synchronous in a plain (non-UnityTest) EditMode test.
            interactable = pawn.gameObject.AddComponent<PawnInteractable>();
            interactable.Pawn = pawn;
            pawn.Spawn();
        }

        [TearDown]
        public void TearDown()
        {
            if (pawn != null) Object.DestroyImmediate(pawn.gameObject);
        }

        [Test]
        public void Transform_IsThePawnsOwnTransform()
        {
            Assert.AreSame(pawn.transform, interactable.Transform);
        }

        [Test]
        public void TryGet_ForwardsToThePawnsCapabilityLookup()
        {
            var dialogue = pawn.gameObject.AddComponent<FakeDialogueTarget>();

            Assert.IsTrue(interactable.TryGet<FakeDialogueTarget>(out var found));
            Assert.AreSame(dialogue, found);
        }

        [Test]
        public void TryGet_ForAMissingCapability_ReturnsFalse()
        {
            Assert.IsFalse(interactable.TryGet<FakeDialogueTarget>(out var found));
            Assert.IsNull(found);
        }

        // The "leave Pawn empty to find it via Awake" fallback is not independently unit tested
        // here, matching PawnInputTarget and DamageReceiver elsewhere in this package: Awake's
        // firing timing after AddComponent is not guaranteed synchronous in a plain EditMode
        // [Test], so these tests assign Pawn explicitly instead (see SetUp) and trust Unity's own
        // GetComponentInParent to do what it always does.

        [Test]
        public void CapabilityGating_ReadsPawnStateItself()
        {
            var lootable = pawn.gameObject.AddComponent<FakeStateGatedLootable>();

            Assert.IsFalse(lootable.TryCollect(), "The capability, not the bridge, decides it is not collectible while alive.");

            pawn.Kill();

            Assert.IsTrue(lootable.TryCollect(), "Once dead, the same component now allows collection — no change needed on the bridge.");
        }

        private sealed class FakeDialogueTarget : MonoBehaviour
        {
        }

        private sealed class FakeStateGatedLootable : MonoBehaviour
        {
            // Looked up lazily rather than cached in Awake, so this works the same whether or not
            // Awake has had a chance to run yet — see the note in SetUp above.
            public bool TryCollect() => GetComponent<Pawn>().State == PawnState.Dead;
        }
    }
}
