using Moq;
using Orbuculum.Instructions;
using NUnit.Framework;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using System.Collections.Generic;
using FluentAssertions;

namespace Orbuculum.Test {
    [TestFixture]
    public class ScryTest {
        [Test]
        public void NoNextTargetExists_IsOnlyItem() {
            var parent = new Mock<ISequenceContainer>();

            var current = new Mock<ISequenceContainer>();
            current.SetupGet(x => x.Parent).Returns(parent.Object);


            parent.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>() { current.Object });

            var sut = Scry.NextTarget(current.Object);

            sut.Should().BeNull();
        }
        [Test]
        public void NoNextTargetExists_IsLastItem() {
            var parent = new Mock<ISequenceContainer>();

            var current = new Mock<ISequenceContainer>();
            current.SetupGet(x => x.Parent).Returns(parent.Object);


            parent.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>() { new Mock<ISequenceItem>().Object, new Mock<ISequenceContainer>().Object, current.Object });

            var sut = Scry.NextTarget(current.Object);

            sut.Should().BeNull();
        }
        [Test]
        public void NoNextTargetExists_IsFirstItem() {
            var parent = new Mock<ISequenceContainer>();

            var current = new Mock<ISequenceContainer>();
            current.SetupGet(x => x.Parent).Returns(parent.Object);


            parent.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>() { current.Object, new Mock<ISequenceItem>().Object, new Mock<ISequenceContainer>().Object });

            var sut = Scry.NextTarget(current.Object);

            sut.Should().BeNull();
        }



        [Test]
        public void NextTargetExists_IsOnSameLevel_DirectlyNext() {
            var parent = new Mock<ISequenceContainer>();

            var current = new Mock<ISequenceContainer>();
            current.SetupGet(x => x.Parent).Returns(parent.Object);

            var next = new Mock<IDeepSkyObjectContainer>();
            next.Setup(x => x.Parent).Returns(parent.Object);


            parent.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>() { current.Object, next.Object });


            var sut = Scry.NextTarget(current.Object);

            sut.Should().Be(next.Object);
        }
        [Test]
        public void NextTargetExists_IsOnSameLevel_SomeItemsInBetween() {
            var parent = new Mock<ISequenceContainer>();

            var current = new Mock<ISequenceContainer>();
            current.SetupGet(x => x.Parent).Returns(parent.Object);

            var next = new Mock<IDeepSkyObjectContainer>();
            next.Setup(x => x.Parent).Returns(parent.Object);


            parent.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>() { current.Object, new Mock<ISequenceContainer>().Object, new Mock<ISequenceItem>().Object, next.Object });


            var sut = Scry.NextTarget(current.Object);

            sut.Should().Be(next.Object);
        }

        [Test]
        public void NextTargetExists_IsOnHigherLevel_DirectlyNext() {
            var root = new Mock<ISequenceContainer>();
            var parent = new Mock<ISequenceContainer>();
            parent.Setup(x => x.Parent).Returns(root.Object);

            var current = new Mock<ISequenceContainer>();
            current.SetupGet(x => x.Parent).Returns(parent.Object);

            var next = new Mock<IDeepSkyObjectContainer>();
            next.Setup(x => x.Parent).Returns(parent.Object);


            parent.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>() { current.Object });
            root.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>() { parent.Object, next.Object });


            var sut = Scry.NextTarget(current.Object);

            sut.Should().Be(next.Object);
        }

        [Test]
        public void NextTargetExists_IsOnHigherLevel_SomeItemsInBetween() {
            var root = new Mock<ISequenceContainer>();
            var parent = new Mock<ISequenceContainer>();
            parent.Setup(x => x.Parent).Returns(root.Object);

            var current = new Mock<ISequenceContainer>();
            current.SetupGet(x => x.Parent).Returns(parent.Object);

            var next = new Mock<IDeepSkyObjectContainer>();
            next.Setup(x => x.Parent).Returns(parent.Object);


            parent.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>() { current.Object, new Mock<ISequenceContainer>().Object, new Mock<ISequenceItem>().Object, next.Object });
            root.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>() { parent.Object, new Mock<ISequenceContainer>().Object, new Mock<ISequenceItem>().Object, next.Object });


            var sut = Scry.NextTarget(current.Object);

            sut.Should().Be(next.Object);
        }



        [Test]
        public void NextTargetExists_IsOnLowerLevel_DirectlyNext() {
            var parent = new Mock<ISequenceContainer>();

            var current = new Mock<ISequenceContainer>();
            current.SetupGet(x => x.Parent).Returns(parent.Object);

            var nextParent = new Mock<ISequenceContainer>();
            nextParent.Setup(x => x.Parent).Returns(parent.Object);
            var next = new Mock<IDeepSkyObjectContainer>();
            nextParent.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>() { next.Object });
            next.Setup(x => x.Parent).Returns(nextParent.Object);


            parent.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>() { current.Object, nextParent.Object });


            var sut = Scry.NextTarget(current.Object);

            sut.Should().Be(next.Object);
        }

        [Test]
        public void NextTargetExists_IsOnLowerLevel_SomeItemsInBetween() {
            var parent = new Mock<ISequenceContainer>();

            var current = new Mock<ISequenceContainer>();
            current.SetupGet(x => x.Parent).Returns(parent.Object);

            var nextParent = new Mock<ISequenceContainer>();
            nextParent.Setup(x => x.Parent).Returns(parent.Object);
            var next = new Mock<IDeepSkyObjectContainer>();
            nextParent.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>() { next.Object });
            next.Setup(x => x.Parent).Returns(nextParent.Object);


            parent.Setup(x => x.GetItemsSnapshot()).Returns(new List<ISequenceItem>() { current.Object, new Mock<ISequenceContainer>().Object, new Mock<ISequenceItem>().Object, nextParent.Object });


            var sut = Scry.NextTarget(current.Object);

            sut.Should().Be(next.Object);
        }


    }
}