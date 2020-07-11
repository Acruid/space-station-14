using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Map;

namespace Robust.UnitTesting.Server.GameObjects.Components
{
    [TestFixture, Parallelizable, TestOf(typeof(ContainerManagerComponent))]
    public class Container_Tests
    {
        private static TestingServerSimulation SimulationFactory()
        {
            var sim = new TestingServerSimulation();

            sim.Setup(
                null,
                compFactory =>
                {
                    compFactory.Register<ContainerManagerComponent>();
                    compFactory.RegisterReference<ContainerManagerComponent, IContainerManager>();
                },
                protoMan => { protoMan.LoadFromStream(new StringReader(PROTOTYPES)); },
                systemMan => { systemMan.LoadExtraSystemType<ContainerSystem>(); });

            return sim;
        }

        private const string PROTOTYPES = @"
- type: entity
  id: dummy
- type: entity
  id: dummyContainer
  components:
  - type: ContainerContainer";

        [Test]
        public void ContainerSerialization()
        {
            var sim = SimulationFactory();

            var mapMan = sim.Collection.Resolve<IMapManager>();
            var entMan = sim.Collection.Resolve<IEntityManager>();

            mapMan.CreateMap(new MapId(1));

            var containerEnt = entMan.SpawnEntity("dummyContainer", new MapCoordinates(0, 0, new MapId(1)));
            var containerComp = containerEnt.GetComponent<ContainerManagerComponent>();
            var container = containerComp.MakeContainer<Container>("testContainer");

            var containeeEnt = entMan.SpawnEntity("dummy", new MapCoordinates(5, 5, new MapId(1)));
            container.Insert(containeeEnt);

            Assert.AreEqual(containerEnt.Transform.MapPosition, containeeEnt.Transform.MapPosition);
            Assert.AreEqual(containerEnt.Uid, containeeEnt.Transform.ParentUid);
        }

        [Test]
        public void TestCreation()
        {
            var sim = SimulationFactory();

            var mapMan = sim.Collection.Resolve<IMapManager>();
            var entMan = sim.Collection.Resolve<IEntityManager>();

            mapMan.CreateMap(new MapId(1));

            var entity = entMan.SpawnEntity("dummy", new GridCoordinates(0, 0, new GridId(1)));

            var container = ContainerManagerComponent.Create<Container>("dummy", entity);

            Assert.That(container.ID, NUnit.Framework.Is.EqualTo("dummy"));
            Assert.That(container.Owner, NUnit.Framework.Is.EqualTo(entity));

            var manager = entity.GetComponent<IContainerManager>();

            Assert.That(container.Manager, NUnit.Framework.Is.EqualTo(manager));
            Assert.That(() => ContainerManagerComponent.Create<Container>("dummy", entity), Throws.ArgumentException);

            Assert.That(manager.HasContainer("dummy2"), NUnit.Framework.Is.False);
            var container2 = ContainerManagerComponent.Create<Container>("dummy2", entity);

            Assert.That(container2.Manager, NUnit.Framework.Is.EqualTo(manager));
            Assert.That(container2.Owner, NUnit.Framework.Is.EqualTo(entity));
            Assert.That(container2.ID, NUnit.Framework.Is.EqualTo("dummy2"));

            Assert.That(manager.HasContainer("dummy"), NUnit.Framework.Is.True);
            Assert.That(manager.HasContainer("dummy2"), NUnit.Framework.Is.True);
            Assert.That(manager.HasContainer("dummy3"), NUnit.Framework.Is.False);

            Assert.That(manager.GetContainer("dummy"), NUnit.Framework.Is.EqualTo(container));
            Assert.That(manager.GetContainer("dummy2"), NUnit.Framework.Is.EqualTo(container2));
            Assert.That(() => manager.GetContainer("dummy3"), Throws.TypeOf<KeyNotFoundException>());

            entity.Delete();

            Assert.That(manager.Deleted, NUnit.Framework.Is.True);
            Assert.That(container.Deleted, NUnit.Framework.Is.True);
            Assert.That(container2.Deleted, NUnit.Framework.Is.True);
        }

        [Test]
        public void TestInsertion()
        {
            var sim = SimulationFactory();

            var mapMan = sim.Collection.Resolve<IMapManager>();
            var entMan = sim.Collection.Resolve<IEntityManager>();

            mapMan.CreateMap(new MapId(1));

            var owner = entMan.SpawnEntity("dummy", new GridCoordinates(0, 0, new GridId(1)));
            var inserted = entMan.SpawnEntity("dummy", new GridCoordinates(0, 0, new GridId(1)));
            var transform = inserted.Transform;

            var container = ContainerManagerComponent.Create<Container>("dummy", owner);
            Assert.That(container.Insert(inserted), NUnit.Framework.Is.True);
            Assert.That(transform.Parent!.Owner, NUnit.Framework.Is.EqualTo(owner));

            var container2 = ContainerManagerComponent.Create<Container>("dummy", inserted);
            Assert.That(container2.Insert(owner), NUnit.Framework.Is.False);

            var success = container.Remove(inserted);
            Assert.That(success, NUnit.Framework.Is.True);

            success = container.Remove(inserted);
            Assert.That(success, NUnit.Framework.Is.False);

            container.Insert(inserted);
            owner.Delete();
            // Make sure inserted was detached.
            Assert.That(transform.Deleted, NUnit.Framework.Is.True);
        }
    }
}
