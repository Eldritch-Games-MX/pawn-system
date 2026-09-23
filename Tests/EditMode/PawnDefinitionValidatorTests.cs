using System.Collections.Generic;
using EldritchGames.PawnSystem.Editor;
using EldritchGames.PawnSystem.Identity;
using EldritchGames.PawnSystem.TestUtilities;
using EldritchGames.PawnSystem.Vitals;
using NUnit.Framework;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    public class PawnDefinitionValidatorTests
    {
        [Test]
        public void Validate_OnASoundDefinition_FindsNothing()
        {
            PawnDefinition definition = new PawnDefinitionBuilder()
                .WithId("knight")
                .WithMaxVital(100f)
                .WithTags("Class.Knight")
                .Build();

            Assert.AreEqual(0, PawnDefinitionValidator.Validate(definition).Count);
        }

        [Test]
        public void Validate_WithAnEmptyId_ReportsIt()
        {
            PawnDefinition definition = new PawnDefinitionBuilder().WithId(string.Empty).WithMaxVital(10f).Build();

            Assert.IsTrue(ContainsMessagePart(PawnDefinitionValidator.Validate(definition), "Id is empty"),
                "An empty Id breaks save restoration, so it must be reported while authoring.");
        }

        [Test]
        public void Validate_WithNonPositiveMaxVital_ReportsIt()
        {
            PawnDefinition definition = new PawnDefinitionBuilder().WithId("a").WithMaxVital(0f).Build();

            Assert.IsTrue(ContainsMessagePart(PawnDefinitionValidator.Validate(definition), "Max Vital"));
        }

        [Test]
        public void Validate_WithDuplicateTags_ReportsTheDuplicate()
        {
            PawnDefinition definition = new PawnDefinitionBuilder()
                .WithId("a")
                .WithMaxVital(10f)
                .WithTags("Class.Rogue", "Class.Rogue")
                .Build();

            Assert.IsTrue(ContainsMessagePart(PawnDefinitionValidator.Validate(definition), "listed more than once"));
        }

        [Test]
        public void Validate_WithAnEmptyModifierSlot_ReportsIt()
        {
            PawnDefinition definition = new PawnDefinitionBuilder()
                .WithId("a")
                .WithMaxVital(10f)
                .WithDamageModifiers(new Vitals.IDamageModifier[] { null })
                .Build();

            Assert.IsTrue(ContainsMessagePart(PawnDefinitionValidator.Validate(definition), "Damage Modifier 0 is empty"));
        }

        [Test]
        public void Validate_WithNegativeIncapacitationDuration_ReportsIt()
        {
            PawnDefinition definition = new PawnDefinitionBuilder()
                .WithId("a").WithMaxVital(10f).WithIncapacitationDuration(-1f).Build();

            Assert.IsTrue(ContainsMessagePart(PawnDefinitionValidator.Validate(definition), "Incapacitation Duration"));
        }

        [Test]
        public void Validate_WithAnEmptyResourceSlot_ReportsIt()
        {
            PawnDefinition definition = new PawnDefinitionBuilder()
                .WithId("a").WithMaxVital(10f)
                .WithInitialResources(new ResourceDefinition[] { null }).Build();

            Assert.IsTrue(ContainsMessagePart(PawnDefinitionValidator.Validate(definition), "Initial Resource 0 is empty"));
        }

        [Test]
        public void Validate_WithDuplicateResources_ReportsTheDuplicate()
        {
            ResourceDefinition mana = TestResources.Create("mana");
            PawnDefinition definition = new PawnDefinitionBuilder()
                .WithId("a").WithMaxVital(10f)
                .WithInitialResources(mana, mana).Build();

            Assert.IsTrue(ContainsMessagePart(PawnDefinitionValidator.Validate(definition), "listed more than once"));
        }

        [Test]
        public void Validate_WithANullDefinition_ReportsOneIssueInsteadOfThrowing()
        {
            List<PawnDefinitionValidator.Issue> issues = PawnDefinitionValidator.Validate(null);

            Assert.AreEqual(1, issues.Count);
        }

        private static bool ContainsMessagePart(List<PawnDefinitionValidator.Issue> issues, string part)
        {
            foreach (PawnDefinitionValidator.Issue issue in issues)
                if (issue.Message.Contains(part))
                    return true;

            return false;
        }
    }
}
