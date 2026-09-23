using System.Collections;
using EldritchGames.PawnSystem.Animation;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.TestUtilities;
using EldritchGames.PawnSystem.Vitals;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EldritchGames.PawnSystem.Tests.PlayMode
{
    public class AnimatorPawnBridgeTests
    {
        private Pawn pawn;

        [TearDown]
        public void TearDown()
        {
            if (pawn != null) Object.Destroy(pawn.gameObject);
        }

        [UnityTest]
        public IEnumerator Bridge_FindsItsPawnAndAnimatorAutomatically()
        {
            pawn = TestPawns.CreateSpawned(new PawnDefinitionBuilder().WithId("a").Build(), "Animated");
            Animator animator = pawn.gameObject.AddComponent<Animator>();
            AnimatorPawnBridge bridge = pawn.gameObject.AddComponent<AnimatorPawnBridge>();

            yield return null;

            Assert.AreSame(pawn, bridge.Pawn);
            Assert.AreSame(animator, bridge.TargetAnimator);
        }

        [UnityTest]
        public IEnumerator FullLifecycle_WithNoAnimatorController_NeverThrows()
        {
            pawn = TestPawns.CreateSpawned(new PawnDefinitionBuilder().WithId("a").WithMaxVital(10f).Build(), "Animated");
            pawn.gameObject.AddComponent<Animator>();
            pawn.gameObject.AddComponent<AnimatorPawnBridge>();

            yield return null;

            // An Animator with no controller makes every Set* call a safe no-op — this exercises
            // every event path the bridge listens to without needing an authored controller asset.
            Assert.DoesNotThrow(() =>
            {
                pawn.ApplyDamage(new DamageInfo(3f));
                pawn.ApplyDamage(new DamageInfo(100f));
                pawn.TryRevive(ReviveInfo.Full);
                pawn.Despawn();
            });

            yield return null;
        }

        [UnityTest]
        public IEnumerator DestroyingTheBridge_UnsubscribesFromThePawn()
        {
            pawn = TestPawns.CreateSpawned(new PawnDefinitionBuilder().WithId("a").Build(), "Animated");
            pawn.gameObject.AddComponent<Animator>();
            var bridge = pawn.gameObject.AddComponent<AnimatorPawnBridge>();

            yield return null;

            Object.Destroy(bridge);
            yield return null;

            // If OnDisable failed to unsubscribe, this would throw against a destroyed component.
            Assert.DoesNotThrow(() => pawn.Kill());
        }
    }
}
