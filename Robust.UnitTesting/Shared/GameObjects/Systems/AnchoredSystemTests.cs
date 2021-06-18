using System;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects.Systems
{
    [TestFixture, Parallelizable]
    public class AnchoredSystemTests
    {
        private static readonly MapId TestMapId = new(1);
        private static readonly GridId TestGridId = new(1);

        private class Subscriber : IEntityEventSubscriber { }

        private static ISimulation SimulationFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .RegisterComponents(factory =>
                {
                    factory.RegisterClass<ContainerManagerComponent>();
                })
                //.RegisterEntitySystems((f => f.LoadExtraSystemType<ContainerSystem>()))
                .InitializeInstance();

            var mapManager = sim.Resolve<IMapManager>();

            // Adds the map with id 1, and spawns entity 1 as the map entity.
            mapManager.CreateMap(TestMapId);

            // Add grid 1, as the default grid to anchor things to.
            mapManager.CreateGrid(TestMapId, TestGridId);

            return sim;
        }

        //TODO: An entity is anchored to the tile it is over on the target grid.
        // An entity is anchored by setting the flag on the transform.
        // An anchored entity is defined as an entity with the ITransformComponent.Anchored flag set.
        // The Anchored field is used for serialization of anchored state.
        // The grid anchor/unanchor functions are internal, expose the query functions to content

        // Unanchoring an entity should leave it parented to the grid it was anchored to.
        // Unanchoring an entity that isn't anchored should be a nop.

        /// <summary>
        /// When an entity is anchored to a grid tile, it's world position is centered on the tile.
        /// Otherwise you can anchor an entity to a tile without the entity actually being on top of the tile.
        /// </summary>
        [Test]
        public void OnAnchored_WorldPosition_TileCenter()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);

            var subscriber = new Subscriber();
            int calledCount = 0;
            var coordinates = new MapCoordinates(new Vector2(7, 7), TestMapId);
            var ent1 = entMan.SpawnEntity(null, coordinates); // this raises MoveEvent, subscribe after
            entMan.EventBus.SubscribeEvent<MoveEvent>(EventSource.Local, subscriber, MoveEventHandler);

            // Act
            var tileIndices = grid.TileIndicesFor(ent1.Transform.Coordinates);
            grid.AddToSnapGridCell(tileIndices, ent1.Uid);

            Assert.That(ent1.Transform.WorldPosition, Is.EqualTo(new Vector2(7.5f, 7.5f)));
            Assert.That(calledCount, Is.EqualTo(0));
            void MoveEventHandler(MoveEvent ev)
            {
                Assert.Fail("MoveEvent raised when anchoring entity.");
                calledCount++;
            }
        }

        /// <summary>
        /// When an entity is anchored to a grid tile, it's local rotation is rounded to the nearest cardinal 4 direction.
        /// </summary>
        [Test]
        public void OnAnchored_WorldRotation_Cardinal()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);

            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            ent1.Transform.LocalRotation = Angle.FromDegrees(60);

            // Act
            var tileIndices = grid.TileIndicesFor(ent1.Transform.Coordinates);
            grid.AddToSnapGridCell(tileIndices, ent1.Uid);

            Assert.That(ent1.Transform.LocalRotation.Degrees, Is.EqualTo(90));
        }

        /// <summary>
        /// When an entity is anchored to a grid tile, it's parent is set to the grid.
        /// </summary>
        [Test]
        public void OnAnchored_Parent_SetToGrid()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);

            var subscriber = new Subscriber();
            int calledCount = 0;
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId)); // this raises MoveEvent, subscribe after
            entMan.EventBus.SubscribeEvent<EntParentChangedMessage>(EventSource.Local, subscriber, ParentChangedHandler);

            // Act
            var tileIndices = grid.TileIndicesFor(ent1.Transform.Coordinates);
            grid.AddToSnapGridCell(tileIndices, ent1.Uid);

            Assert.That(ent1.Transform.ParentUid, Is.EqualTo(grid.GridEntityId));
            Assert.That(calledCount, Is.EqualTo(1));
            void ParentChangedHandler(EntParentChangedMessage ev)
            {
                Assert.That(ev.Entity, Is.EqualTo(ent1));
                calledCount++;
            }
        }

        /// <summary>
        /// Entities cannot be anchored to empty tiles. Attempting this is a no-op, and silently fails.
        /// </summary>
        [Test]
        public void OnAnchored_EmptyTile_Nop()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            var tileIndices = grid.TileIndicesFor(ent1.Transform.Coordinates);
            grid.SetTile(tileIndices, Tile.Empty);

            // Act
            grid.AddToSnapGridCell(tileIndices, ent1.Uid);

            Assert.That(grid.GetAnchoredEntities(tileIndices).Count(), Is.EqualTo(0));
            Assert.That(grid.GetTileRef(tileIndices).Tile, Is.EqualTo(Tile.Empty));
        }

        /// <summary>
        /// Entities can be anchored to any non-empty grid tile. A physics component is not required on either
        /// the grid or the entity to anchor it.
        /// </summary>
        [Test]
        public void OnAnchored_NonEmptyTile_Anchors()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            var tileIndices = grid.TileIndicesFor(ent1.Transform.Coordinates);
            grid.SetTile(tileIndices, new Tile(1));

            // Act
            grid.AddToSnapGridCell(tileIndices, ent1.Uid);

            Assert.That(grid.GetAnchoredEntities(tileIndices).First(), Is.EqualTo(ent1.Uid));
            Assert.That(grid.GetTileRef(tileIndices).Tile, Is.Not.EqualTo(Tile.Empty));
            Assert.That(ent1.HasComponent<PhysicsComponent>(), Is.False);
            Assert.That(entMan.GetEntity(grid.GridEntityId).HasComponent<PhysicsComponent>(), Is.False);
        }

        /// <summary>
        /// Local position of an anchored entity cannot be changed (can still change world position via parent).
        /// Writing to the property is a no-op and is silently ignored.
        /// Because the position cannot be changed, MoveEvents are not raised when setting the property.
        /// </summary>
        [Test]
        public void Anchored_SetPosition_Nop()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);

            var subscriber = new Subscriber();
            int calledCount = 0;
            var coordinates = new MapCoordinates(new Vector2(7, 7), TestMapId);
            var ent1 = entMan.SpawnEntity(null, coordinates); // this raises MoveEvent, subscribe after
            entMan.EventBus.SubscribeEvent<MoveEvent>(EventSource.Local, subscriber, MoveEventHandler);

            var tileIndices = grid.TileIndicesFor(ent1.Transform.Coordinates);
            grid.AddToSnapGridCell(tileIndices, ent1.Uid);

            // Act
            ent1.Transform.WorldPosition = new Vector2(99, 99);
            ent1.Transform.LocalPosition = new Vector2(99, 99);
            ent1.Transform.Coordinates = new EntityCoordinates(grid.GridEntityId, 99, 99); // make sure not to change parent, that would un-anchor

            Assert.That(ent1.Transform.MapPosition, Is.EqualTo(coordinates));
            Assert.That(calledCount, Is.EqualTo(0));
            void MoveEventHandler(MoveEvent ev)
            {
                Assert.Fail("MoveEvent raised when entity is anchored.");
                calledCount++;
            }
        }

        /// <summary>
        /// Local rotation of an anchored entity cannot be changed (can still rotate/orbit in word via parent).
        /// Writing to the property is a no-op and is silently ignored.
        /// Because the position cannot be changed, RotateEvents are not raised when setting the property.
        /// </summary>
        [Test]
        public void Anchored_SetRotation_Nop()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);

            var subscriber = new Subscriber();
            int calledCount = 0;
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            ent1.Transform.NoLocalRotation = false;
            var testAngle = Angle.FromDegrees(90);
            ent1.Transform.LocalRotation = testAngle;

            var tileIndices = grid.TileIndicesFor(ent1.Transform.Coordinates);
            grid.AddToSnapGridCell(tileIndices, ent1.Uid);
            entMan.EventBus.SubscribeEvent<RotateEvent>(EventSource.Local, subscriber, MoveEventHandler);

            // Act
            ent1.Transform.WorldRotation = Angle.FromDegrees(180);
            ent1.Transform.LocalRotation = Angle.FromDegrees(180);

            Assert.That(ent1.Transform.LocalRotation, Is.EqualTo(testAngle));
            Assert.That(calledCount, Is.EqualTo(0));
            void MoveEventHandler(RotateEvent ev)
            {
                Assert.Fail("RotateEvent raised when entity is anchored.");
                calledCount++;
            }
        }

        /// <summary>
        /// Changing the parent of the entity un-anchors it.
        /// </summary>
        [Test]
        public void Anchored_ChangeParent_Unanchors()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            var tileIndices = grid.TileIndicesFor(ent1.Transform.Coordinates);
            grid.SetTile(tileIndices, new Tile(1));
            grid.AddToSnapGridCell(tileIndices, ent1.Uid);

            // Act
            ent1.Transform.ParentUid = mapMan.GetMapEntityId(TestMapId);

            Assert.That(grid.GetAnchoredEntities(tileIndices).Count(), Is.EqualTo(0));
            Assert.That(grid.GetTileRef(tileIndices).Tile, Is.EqualTo(new Tile(1)));
        }

        /// <summary>
        /// Setting the parent of an anchored entity to the same parent is a no-op (it will not be un-anchored).
        /// This is an specific case to the base functionality of TransformComponent, where in general setting the same
        /// parent is a no-op.
        /// </summary>
        [Test]
        public void Anchored_SetParentSame_Nop()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            var tileIndices = grid.TileIndicesFor(ent1.Transform.Coordinates);
            grid.SetTile(tileIndices, new Tile(1));
            grid.AddToSnapGridCell(tileIndices, ent1.Uid);

            // Act
            ent1.Transform.ParentUid = grid.GridEntityId;

            Assert.That(grid.GetAnchoredEntities(tileIndices).First(), Is.EqualTo(ent1.Uid));
            Assert.That(grid.GetTileRef(tileIndices).Tile, Is.Not.EqualTo(Tile.Empty));
        }

        /// <summary>
        /// If a tile is changed to a space tile, all entities anchored to that tile are unanchored.
        /// </summary>
        [Test]
        public void Anchored_TileToSpace_Unanchors()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            var tileIndices = grid.TileIndicesFor(ent1.Transform.Coordinates);
            grid.SetTile(tileIndices, new Tile(1));
            grid.AddToSnapGridCell(tileIndices, ent1.Uid);

            // Act
            grid.SetTile(tileIndices, Tile.Empty);

            Assert.That(grid.GetAnchoredEntities(tileIndices).Count(), Is.EqualTo(0));
            Assert.That(grid.GetTileRef(tileIndices).Tile, Is.EqualTo(new Tile(1)));
        }

        /// <summary>
        /// Adding an anchored entity to a container un-anchors an entity. There should be no way to have an anchored entity
        /// inside a container.
        /// </summary>
        /// <remarks>
        /// The only way you can do this without changing the parent is to make the parent grid a ContainerManager, then add the anchored entity to it.
        /// </remarks>
        [Test]
        public void Anchored_AddToContainer_Unanchors()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            var tileIndices = grid.TileIndicesFor(ent1.Transform.Coordinates);
            grid.SetTile(tileIndices, new Tile(1));
            grid.AddToSnapGridCell(tileIndices, ent1.Uid);

            // Act
            var gridEnt = entMan.GetEntity(grid.GridEntityId);
            var containerMan = gridEnt.AddComponent<ContainerManagerComponent>();
            var container = containerMan.MakeContainer<Container>("TestContainer");
            container.Insert(ent1);

            Assert.That(grid.GetAnchoredEntities(tileIndices).Count(), Is.EqualTo(0));
            Assert.That(grid.GetTileRef(tileIndices).Tile, Is.EqualTo(new Tile(1)));
        }

        // PhysicsComponent.Anchored is obsolete,
        // And PhysicsComponent.BodyType is not able to be changed by content. PhysicsComponent.BodyType is synchronized with ITransformComponent.Anchored
        // through anchored messages. SnapGridComponent is obsolete.

        /// <summary>
        /// Adding a physics component should poll ITransformComponent.Anchored for the correct body type.
        /// </summary>
        [Test]
        public void Anchored_AddPhysComp_IsStaticBody()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            var tileIndices = grid.TileIndicesFor(ent1.Transform.Coordinates);
            grid.SetTile(tileIndices, new Tile(1));
            grid.AddToSnapGridCell(tileIndices, ent1.Uid);

            // Act
            var physComp = ent1.AddComponent<PhysicsComponent>();

            Assert.That(physComp.BodyType, Is.EqualTo(BodyType.Static));
        }

        /// <summary>
        /// When an entity is anchored, it's physics body type is set to <see cref="BodyType.Static"/>.
        /// </summary>
        [Test]
        public void OnAnchored_HasPhysicsComp_IsStaticBody()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            var tileIndices = grid.TileIndicesFor(ent1.Transform.Coordinates);
            grid.SetTile(tileIndices, new Tile(1));
            var physComp = ent1.AddComponent<PhysicsComponent>();
            physComp.BodyType = BodyType.Dynamic;

            // Act
            grid.AddToSnapGridCell(tileIndices, ent1.Uid);

            Assert.That(physComp.BodyType, Is.EqualTo(BodyType.Static));
        }

        /// <summary>
        /// When an entity is unanchored, it's physics body type is set to <see cref="BodyType.Dynamic"/>.
        /// </summary>
        [Test]
        public void OnUnanchored_HasPhysicsComp_IsDynamicBody()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var grid = mapMan.GetGrid(TestGridId);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(new Vector2(7, 7), TestMapId));
            var tileIndices = grid.TileIndicesFor(ent1.Transform.Coordinates);
            grid.SetTile(tileIndices, new Tile(1));
            var physComp = ent1.AddComponent<PhysicsComponent>();
            grid.AddToSnapGridCell(tileIndices, ent1.Uid);

            // Act
            grid.RemoveFromSnapGridCell(tileIndices, ent1.Uid);

            Assert.That(physComp.BodyType, Is.EqualTo(BodyType.Dynamic));
        }
    }
}
