using EldritchGames.PawnSystem.Identity;
using NUnit.Framework;

namespace EldritchGames.PawnSystem.Tests.EditMode
{
    public class PawnTagContainerTests
    {
        private PawnTagContainer tags;

        [SetUp]
        public void SetUp()
        {
            tags = new PawnTagContainer();
        }

        [Test]
        public void Has_MatchesParentTagsHierarchically()
        {
            tags.Add("Status.Stunned");

            Assert.IsTrue(tags.Has("Status.Stunned"), "An exact tag matches.");
            Assert.IsTrue(tags.Has("Status"), "A parent tag matches its children.");
            Assert.IsFalse(tags.Has("Status.Burning"), "A sibling tag does not match.");
            Assert.IsFalse(tags.Has("Stunned"), "Matching is by dotted prefix, not by substring.");
        }

        [Test]
        public void Add_TheSameTagTwice_ChangesNothingTheSecondTime()
        {
            int raised = 0;
            tags.TagAdded += _ => raised++;

            Assert.IsTrue(tags.Add("Class.Rogue"));
            Assert.IsFalse(tags.Add("Class.Rogue"));
            Assert.AreEqual(1, tags.Count);
            Assert.AreEqual(1, raised, "Adding a tag that is already present raises nothing.");
        }

        [Test]
        public void Add_AnEmptyTag_IsRejected()
        {
            Assert.IsFalse(tags.Add(default));
            Assert.IsFalse(tags.Add("   "));
            Assert.AreEqual(0, tags.Count);
        }

        [Test]
        public void Remove_MatchesExactlyAndNotHierarchically()
        {
            tags.Add("Status.Stunned");

            Assert.IsFalse(tags.Remove("Status"), "Removing a parent does not remove its children.");
            Assert.IsTrue(tags.Remove("Status.Stunned"));
            Assert.AreEqual(0, tags.Count);
        }

        [Test]
        public void Remove_RaisesTagRemovedOnlyOnAChange()
        {
            int raised = 0;
            tags.TagRemoved += _ => raised++;

            tags.Add("A");
            tags.Remove("A");
            tags.Remove("A");

            Assert.AreEqual(1, raised);
        }

        [Test]
        public void HasAny_And_HasAll_ApplyTheSameHierarchicalMatch()
        {
            tags.Add("Status.Stunned");
            tags.Add("Class.Rogue");

            Assert.IsTrue(tags.HasAny(new PawnTag[] { "Faction.Guild", "Status" }));
            Assert.IsFalse(tags.HasAny(new PawnTag[] { "Faction.Guild" }));
            Assert.IsTrue(tags.HasAll(new PawnTag[] { "Status", "Class.Rogue" }));
            Assert.IsFalse(tags.HasAll(new PawnTag[] { "Status", "Faction" }));
        }

        [Test]
        public void HasAll_WithAnEmptyQuery_IsTrue()
        {
            Assert.IsTrue(tags.HasAll(null), "Requiring nothing is always satisfied.");
        }

        [Test]
        public void Clear_RemovesEverythingAndReportsEachRemoval()
        {
            tags.Add("A");
            tags.Add("B");

            int raised = 0;
            tags.TagRemoved += _ => raised++;

            tags.Clear();

            Assert.AreEqual(0, tags.Count);
            Assert.AreEqual(2, raised);
        }

        [Test]
        public void Tag_EqualityIgnoresSurroundingWhitespace()
        {
            Assert.AreEqual(new PawnTag(" Status.Stunned "), new PawnTag("Status.Stunned"));
        }
    }
}
