using System;
using EldritchGames.InputSystem;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.InputSystem;
using EldritchGames.PawnSystem.Movement;
using EldritchGames.PawnSystem.TestUtilities;
using NUnit.Framework;
using UnityEngine;

namespace EldritchGames.PawnSystem.InputSystem.Tests.EditMode
{
    public class ControllerPossessionTests
    {
        private Pawn pawn;
        private PawnInputTarget target;
        private FakeController controller;
        private ControllerPossessor possessor;

        [SetUp]
        public void SetUp()
        {
            PawnDefinition definition = new PawnDefinitionBuilder().WithId("knight").WithMaxVital(50f).Build();
            pawn = TestPawns.Create(definition);
            target = pawn.gameObject.AddComponent<PawnInputTarget>();
            target.Pawn = pawn;

            controller = new FakeController();
            possessor = new ControllerPossessor(controller);

            pawn.Spawn();
        }

        [TearDown]
        public void TearDown()
        {
            if (pawn != null) UnityEngine.Object.DestroyImmediate(pawn.gameObject);
        }

        [Test]
        public void Possessing_PointsTheControllerAtThePawnsInputTarget()
        {
            pawn.TryPossess(possessor);

            Assert.AreSame(target, controller.CurrentPawn, "The controller drives the pawn through its input target.");
            Assert.AreSame(pawn, possessor.CurrentPawn);
        }

        [Test]
        public void Releasing_PointsTheControllerAtTheNullController()
        {
            pawn.TryPossess(possessor);

            pawn.Release();

            Assert.AreSame(NullPawnController.Instance, controller.CurrentPawn,
                "A controller between bodies still has something safe to talk to.");
            Assert.IsNull(possessor.CurrentPawn);
        }

        [Test]
        public void Death_HandsTheBodyBackToTheController()
        {
            pawn.TryPossess(possessor);

            pawn.Kill();

            Assert.AreSame(NullPawnController.Instance, controller.CurrentPawn);
        }

        [Test]
        public void Possessing_APawnWithoutAnInputTarget_ExplainsWhatIsMissing()
        {
            Pawn bare = TestPawns.CreateSpawned(new PawnDefinitionBuilder().WithId("bare").Build(), "Bare");

            try
            {
                Assert.Throws<InvalidOperationException>(() => bare.TryPossess(possessor));
                Assert.IsFalse(bare.IsPossessed, "A refused possession is rolled back rather than half-applied.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bare.gameObject);
            }
        }

        [Test]
        public void Commands_ReachThePawnsCapabilities()
        {
            var motor = new FakePawnMotor();
            pawn.SetMotor(motor);
            pawn.TryPossess(possessor);

            controller.CurrentPawn.ExecuteCommand(new MovePawnCommand(Vector3.forward * 4f, 0.25f));

            Assert.AreEqual(1, motor.MoveCallCount);
            Assert.AreEqual(Vector3.forward * 4f, motor.LastVelocity);
            Assert.AreEqual(0.25f, motor.LastDeltaTime, 0.0001f, "Time is passed through the command, not read from Unity.");
        }

        [Test]
        public void Commands_AreDroppedWhileThePawnIsDead()
        {
            var motor = new FakePawnMotor();
            pawn.SetMotor(motor);
            pawn.TryPossess(possessor);
            pawn.Kill();

            target.ExecuteCommand(new MovePawnCommand(Vector3.forward, 0.25f));

            Assert.AreEqual(0, motor.MoveCallCount, "Input keeps arriving for a frame or two after death; it must not drive a corpse.");
        }

        [Test]
        public void Commands_AreDroppedWhileThePawnIsUnpossessed()
        {
            var motor = new FakePawnMotor();
            pawn.SetMotor(motor);

            target.ExecuteCommand(new MovePawnCommand(Vector3.forward, 0.25f));

            Assert.AreEqual(0, motor.MoveCallCount);
        }

        [Test]
        public void ACommandForAMissingCapability_DoesNothing()
        {
            pawn.TryPossess(possessor);

            Assert.DoesNotThrow(() => target.ExecuteCommand(new MovePawnCommand(Vector3.forward, 0.25f)),
                "A pawn with no motor silently ignores movement commands rather than failing.");
        }

        [Test]
        public void LookCommand_TurnsTheMotor()
        {
            var motor = new FakePawnMotor();
            pawn.SetMotor(motor);
            pawn.TryPossess(possessor);

            target.ExecuteCommand(LookPawnCommand.Towards(Vector3.right));

            Assert.AreEqual(90f, motor.LastLookRotation.eulerAngles.y, 0.001f);
        }

        [Test]
        public void NullPawnController_SwallowsEverything()
        {
            Assert.DoesNotThrow(() => NullPawnController.Instance.ExecuteCommand(new MovePawnCommand(Vector3.one, 1f)));
            Assert.IsFalse(NullPawnController.Instance.TryGet<IPawnMotor>(out _));
        }

        private sealed class FakeController : IController
        {
            public int PlayerIndex { get; private set; }

            public IInputContext CurrentContext { get; private set; }

            public IPawnController CurrentPawn { get; private set; }

            public void Initialize(int playerIndex, IPawnController pawn, IInputContextStack stack)
            {
                PlayerIndex = playerIndex;
                CurrentPawn = pawn;
            }

            public void SetPawn(IPawnController pawn) => CurrentPawn = pawn;

            public void SwitchContext(IInputContext context) => CurrentContext = context;

            public void PushContext(IInputContext context) => CurrentContext = context;

            public void PopContext() => CurrentContext = null;

            public void Tick(float deltaTime)
            {
            }

            public void PushCompletable(ICompletableInputContext context, Action<bool> onCompleted = null)
            {
                CurrentContext = context;
            }
        }
    }
}
