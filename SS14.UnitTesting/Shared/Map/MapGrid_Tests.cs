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
        public void CreateGrid()
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

        [Test]
        public void GridExistsTrue()
        {
            var mapMan = MapManagerFactory();
            var map = mapMan.CreateMap();
            var grid = map.CreateGrid(new GridId(3));

            var result = map.GridExists(grid.Index);

            Assert.That(result, Is.True);
        }

        [Test]
        public void GridExistsFalse()
        {
            var mapMan = MapManagerFactory();
            var map = mapMan.CreateMap();
            var grid = map.CreateGrid(new GridId(7));

            var result = map.GridExists(new GridId(5));

            Assert.That(result, Is.False);
        }

        [Test]
        public void RemoveGridExists()
        {
            var mapMan = MapManagerFactory();
            var map = mapMan.CreateMap(new MapId(7));
            var grid = map.CreateGrid(new GridId(5));

            mapMan.DeleteGrid(new GridId(5));

            var result = map.GridExists(new GridId(5));
            Assert.That(result, Is.False);
        }
    }

    [TestFixture, Parallelizable, TestOf(typeof(MapManager.MapGrid))]
    internal class MapGrid_TileAccess_Tests
    {
        [Test]
        public void GetNonExistTile()
        {
            var grid = MapGridFactory(out var map);

            var tileRef = grid.GetTile(new GridCoordinates(new Vector2(19,21), grid.Index));

            Assert.That(tileRef.Tile.IsEmpty, Is.True);
            Assert.That(tileRef.Tile.Data, Is.EqualTo(0));
            Assert.That(tileRef.Tile.TileId, Is.EqualTo(0));
            Assert.That(tileRef.GridIndex, Is.EqualTo(grid.Index));
            Assert.That(tileRef.MapIndex, Is.EqualTo(map.Index));
            Assert.That(tileRef.GridIndices, Is.EqualTo(new MapIndices(19, 21)));
        }

        /// <summary>
        ///     Just your basic Set and Get Tile.
        /// </summary>
        [Test]
        public void SetAndGetTile()
        {
            var grid = MapGridFactory(out var map);

            grid.SetTile(new GridCoordinates(new Vector2(19,21), grid.Index), 0x9999, 0xAAAA);
            var tileRef = grid.GetTile(new GridCoordinates(new Vector2(19, 21), grid.Index));

            Assert.That(tileRef.Tile.IsEmpty, Is.False);
            Assert.That(tileRef.Tile.Data, Is.EqualTo(0xAAAA));
            Assert.That(tileRef.Tile.TileId, Is.EqualTo(0x9999));
            Assert.That(tileRef.GridIndex, Is.EqualTo(grid.Index));
            Assert.That(tileRef.MapIndex, Is.EqualTo(map.Index));
            Assert.That(tileRef.GridIndices, Is.EqualTo(new MapIndices(19, 21)));
        }

        /// <summary>
        ///     This tests that the GridCoordinates are actually local. 
        /// </summary>
        [Test]
        public void SetAndGetMovedGrid()
        {
            var grid = MapGridFactory(out _);

            grid.WorldPosition = new Vector2(3559, 3571);
            grid.SetTile(new GridCoordinates(new Vector2(19, 23), grid.Index), 0x9999, 0xAAAA);
            var tileRef = grid.GetTile(new GridCoordinates(new Vector2(19, 23), grid.Index));

            Assert.That(tileRef.Tile.IsEmpty, Is.False);
            Assert.That(tileRef.Tile.Data, Is.EqualTo(0xAAAA));
            Assert.That(tileRef.Tile.TileId, Is.EqualTo(0x9999));
        }

        /// <summary>
        ///     This tests that the GridCoordinates are actually made local to this grid, instead of
        ///     just reading the coordinates blindly.
        /// </summary>
        [Test]
        public void SetAndGetTileFromOtherGridPosition()
        {
            var grid = MapGridFactory(out var map);
            var gridOther = map.CreateGrid();

            gridOther.WorldPosition = new Vector2(12, 12);
            var coordinates = new GridCoordinates(new Vector2(7, 11), gridOther.Index);
            
            grid.SetTile(coordinates, 0x9999, 0xAAAA);
            var tileRef = grid.GetTile(new GridCoordinates(new Vector2(19, 23), grid.Index));

            Assert.That(tileRef.Tile.IsEmpty, Is.False);
            Assert.That(tileRef.Tile.Data, Is.EqualTo(0xAAAA));
            Assert.That(tileRef.Tile.TileId, Is.EqualTo(0x9999));
        }
        
        private static IMapGrid MapGridFactory(out IMap map)
        {
            var mapMan = MapGrid_General_Tests.MapManagerFactory();
            map = mapMan.CreateMap();
            return map.CreateGrid();
        }
    }
}
