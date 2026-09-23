using System.Collections;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.Movement;
using EldritchGames.PawnSystem.TestUtilities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EldritchGames.PawnSystem.Tests.PlayMode
{
    public class CharacterControllerMotorTests
    {
        private const float FixedStep = 0.02f;

        private GameObject ground;
        private Pawn pawn;

        [SetUp]
        public void SetUp()
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(50f, 1f, 50f);

            PawnDefinition definition = new PawnDefinitionBuilder().WithId("runner").WithMaxVital(10f).Build();
            pawn = TestPawns.Create(definition, "Runner");
            pawn.gameObject.AddComponent<CharacterController>();
            pawn.gameObject.AddComponent<CharacterControllerMotor>();
        }

        [TearDown]
        public void TearDown()
        {
            if (pawn != null) Object.Destroy(pawn.gameObject);
            if (ground != null) Object.Destroy(ground);
        }

        [UnityTest]
        public IEnumerator Motor_IsFoundThroughTheCapabilityLookup()
        {
            yield return null;

            Assert.IsTrue(pawn.TryGet<IPawnMotor>(out IPawnMotor motor), "A motor component is a capability like any other.");
            Assert.AreSame(pawn.Motor, motor);
        }

        [UnityTest]
        public IEnumerator Spawn_PlacesThePawnThroughTheMotor()
        {
            yield return null;

            pawn.Spawn(new Vector3(6f, 1f, 2f), Quaternion.identity);
            yield return null;

            Assert.AreEqual(6f, pawn.transform.position.x, 0.01f,
                "Placement goes through the motor so the physics controller is moved correctly.");
            Assert.AreEqual(2f, pawn.transform.position.z, 0.01f);
        }

        [UnityTest]
        public IEnumerator Move_TranslatesThePawnAlongTheRequestedVelocity()
        {
            yield return null;
            pawn.Spawn(new Vector3(0f, 1f, 0f), Quaternion.identity);

            float startZ = pawn.transform.position.z;

            // A fixed step, not Time.deltaTime: batch-mode frames are far shorter than real ones,
            // and passing time in is exactly what makes the motor deterministic.
            for (int i = 0; i < 30; i++)
            {
                pawn.Motor.Move(Vector3.forward * 3f, FixedStep);
                yield return null;
            }

            Assert.Greater(pawn.transform.position.z, startZ + 0.5f, "Thirty frames at three units per second moves the pawn forward.");
        }

        [UnityTest]
        public IEnumerator Move_AppliesGravityWhileAirborne()
        {
            yield return null;
            pawn.Spawn(new Vector3(0f, 6f, 0f), Quaternion.identity);

            float startY = pawn.transform.position.y;

            for (int i = 0; i < 30; i++)
            {
                pawn.Motor.Move(Vector3.zero, FixedStep);
                yield return null;
            }

            Assert.Less(pawn.transform.position.y, startY, "A pawn standing still in mid-air still falls.");
        }

        [UnityTest]
        public IEnumerator Teleport_ClearsAccumulatedVelocity()
        {
            yield return null;
            pawn.Spawn(new Vector3(0f, 6f, 0f), Quaternion.identity);

            for (int i = 0; i < 10; i++)
            {
                pawn.Motor.Move(Vector3.forward * 2f, FixedStep);
                yield return null;
            }

            pawn.Motor.Teleport(new Vector3(0f, 1f, 0f), Quaternion.identity);

            Assert.AreEqual(Vector3.zero, pawn.Motor.Velocity, "A teleported pawn must not arrive still falling or still running.");
        }
    }
}
