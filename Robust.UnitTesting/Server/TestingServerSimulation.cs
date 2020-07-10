using System.Threading;
using Moq;
using Robust.Server.GameObjects;
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

namespace Robust.UnitTesting.Server
{
    public class TestingServerSimulation
    {
        public delegate void CompRegistrationDelegate(IComponentFactory factory);

        public delegate void DiContainerDelegate(IDependencyCollection diContainer);

        public delegate void EntitySystemRegistrationDelegate(IEntitySystemManager systemMan);

        public delegate void PrototypeRegistrationDelegate(IPrototypeManager protoMan);

        private static readonly ThreadLocal<ILogManager> LoggerSingleton = new ThreadLocal<ILogManager>();

        public IDependencyCollection Collection { get; private set; } = default!;

        public void Setup(DiContainerDelegate? diDelegate, CompRegistrationDelegate? regDelegate,
            PrototypeRegistrationDelegate? protoDelegate, EntitySystemRegistrationDelegate? systemDelegate)
        {
            if (!LoggerSingleton.IsValueCreated)
            {
                var fakeLogManager = new Mock<ILogManager>();
                var sawmill = new Mock<ISawmill>().Object;
                fakeLogManager.Setup(m => m.GetSawmill(It.IsAny<string>())).Returns(sawmill);
                LoggerSingleton.Value = fakeLogManager.Object;
                Logger.LogManager = fakeLogManager.Object;
            }

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
            container.Register<IPhysicsManager, PhysicsManager>();

            container.RegisterInstance<ILogManager>(LoggerSingleton.Value!);
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
