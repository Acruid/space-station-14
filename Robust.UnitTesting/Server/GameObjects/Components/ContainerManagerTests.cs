using System.IO;
using Moq;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Robust.UnitTesting.Server.GameObjects.Components
{
    [TestFixture]
    public class ContainerManagerTests
    {
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

        private static ServerSim SimulationFactory()
        {
            var sim = new ServerSim();
            sim.Setup(
                null,
                (compFactory =>
                {
                    compFactory.Register<ContainerManagerComponent>();
                    compFactory.RegisterReference<ContainerManagerComponent, IContainerManager>();
                }), (protoMan =>
                {
                    protoMan.LoadFromStream(new StringReader(PROTOTYPES));
                }),
                systemMan =>
                {
                    systemMan.LoadExtraSystemType<ContainerSystem>();
                });

            return sim;
        }

        private const string PROTOTYPES = @"
- type: entity
  id: dummy
- type: entity
  id: dummyContainer
  components:
  - type: ContainerContainer";
    }

    public class ServerSim
    {
        public IDependencyCollection Collection { get; private set; }

        public delegate void DiContainerDelegate(IDependencyCollection diContainer);

        public delegate void CompRegistrationDelegate(IComponentFactory factory);

        public delegate void PrototypeRegistrationDelegate(IPrototypeManager protoMan);

        public delegate void EntitySystemRegistrationDelegate(IEntitySystemManager systemMan);

        public void Setup(DiContainerDelegate diDelegate, CompRegistrationDelegate? regDelegate,
            PrototypeRegistrationDelegate? protoDelegate, EntitySystemRegistrationDelegate systemDelegate)
        {
            var container = new DependencyCollection();
            Collection = container;

            container.Register<IServerEntityManager, ServerEntityManager>();
            container.Register<IEntityManager, ServerEntityManager>();
            container.Register<IComponentManager, ComponentManager>();
            container.Register<IMapManager, MapManager>();
            container.Register<IPrototypeManager, PrototypeManager>();
            container.Register<IComponentFactory, ComponentFactory>();
            container.Register<IEntitySystemManager, EntitySystemManager>();
            container.Register<IDynamicTypeFactory, DynamicTypeFactory>();
            container.Register<ILogManager, LogManager>();
            container.Register<IPhysicsManager, PhysicsManager>();

            container.RegisterInstance<IPauseManager>(new Mock<IPauseManager>().Object);
            container.RegisterInstance<IConfigurationManager>(new Mock<IConfigurationManager>().Object);
            container.RegisterInstance<IEntityNetworkManager>(new Mock<IEntityNetworkManager>().Object);
            container.RegisterInstance<IGameTiming>(new Mock<IGameTiming>().Object);
            container.RegisterInstance<INetManager>(new Mock<INetManager>().Object);
            container.RegisterInstance<IReflectionManager>(new Mock<IReflectionManager>().Object);
            container.RegisterInstance<IResourceManager>(new Mock<IResourceManager>().Object);

            diDelegate?.Invoke(container);
            container.BuildGraph();

            var compFactory = container.Resolve<IComponentFactory>();

            compFactory.Register<MetaDataComponent>();
            compFactory.RegisterReference<MetaDataComponent, IMetaDataComponent>();

            compFactory.Register<TransformComponent>();
            compFactory.RegisterReference<TransformComponent, ITransformComponent>();

            compFactory.Register<MapComponent>();
            compFactory.RegisterReference<MapComponent, IMapComponent>();

            compFactory.Register<MapGridComponent>();
            compFactory.RegisterReference<MapGridComponent, IMapGridComponent>();

            compFactory.Register<CollidableComponent>();
            compFactory.RegisterReference<CollidableComponent, ICollidableComponent>();

            regDelegate?.Invoke(compFactory);

            var entityMan = container.Resolve<IEntityManager>();
            entityMan.Initialize();
            systemDelegate?.Invoke(container.Resolve<IEntitySystemManager>());
            entityMan.Startup();

            var mapManager = container.Resolve<IMapManager>();
            mapManager.Initialize();
            mapManager.Startup();

            var protoMan = container.Resolve<IPrototypeManager>();
            protoMan.RegisterType(typeof(EntityPrototype));
            protoDelegate?.Invoke(protoMan);
            protoMan.Resync();
        }
    }
}
