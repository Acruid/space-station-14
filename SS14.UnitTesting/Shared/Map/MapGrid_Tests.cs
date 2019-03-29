using Moq;
using NUnit.Framework;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.UnitTesting.Shared.Map
{
    [TestFixture, Parallelizable, TestOf(typeof(MapManager.MapGrid))]
    internal class MapGrid_General_Tests
    {
        [Test]
        public void NewGrid()
        {
            var mapMan = MapManagerFactory();
            var map = mapMan.CreateMap(new MapId(7));
            var grid = map.CreateGrid(new GridId(5),16,1);

            Assert.That(grid, Is.Not.Null);
            Assert.That(grid.IsDefaultGrid, Is.False);
            Assert.That(grid.ParentMap, Is.EqualTo(map));
            Assert.That(grid.MapId, Is.EqualTo(new MapId(7)));
            Assert.That(grid.ChunkSize, Is.EqualTo(16));
            Assert.That(grid.SnapSize, Is.EqualTo(1f));
            Assert.That(grid.Index, Is.EqualTo(new GridId(5)));
            Assert.That(grid.TileSize, Is.EqualTo(1));
            Assert.That(grid.WorldPosition, Is.EqualTo(Vector2.Zero));
            Assert.That(grid.ChunkCount, Is.EqualTo(0));
        }

        [Test]
        public void IdentifiesAsDefaultGrid()
        {
            var mapMan = MapManagerFactory();
            var map = mapMan.CreateMap(new MapId(7));
            var grid = map.DefaultGrid;

            Assert.That(grid.IsDefaultGrid, Is.True);
        }
        
        internal static IMapManager MapManagerFactory()
        {
            var container = new DependencyCollection();
            container.RegisterInstance<IGameTiming>(new Mock<IGameTiming>().Object);
            container.RegisterInstance<INetManager>(new Mock<INetManager>().Object);
            container.Register<IMapManager, MapManager>();
            container.BuildGraph();

            return container.Resolve<IMapManager>();
        }
    }

    [TestFixture, Parallelizable, TestOf(typeof(MapManager.MapGrid))]
    internal class MapGrid_TileAccess_Tests
    {
        [Test]
        public void GetNonExistTile()
        {
            var mapMan = MapGrid_General_Tests.MapManagerFactory();
            var map = mapMan.CreateMap();
            var grid = map.CreateGrid();

            var tileRef = grid.GetTile(new GridCoordinates(new Vector2(19,21), grid.Index));

            Assert.That(tileRef.Tile.IsEmpty, Is.True);
            Assert.That(tileRef.Tile.Data, Is.EqualTo(0));
            Assert.That(tileRef.Tile.TileId, Is.EqualTo(0));
            Assert.That(tileRef.GridIndex, Is.EqualTo(grid.Index));
            Assert.That(tileRef.MapIndex, Is.EqualTo(map.Index));
            Assert.That(tileRef.GridIndices, Is.EqualTo(new MapIndices(19, 21)));
        }
    }
}
