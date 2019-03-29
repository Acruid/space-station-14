using NUnit.Framework;
using SS14.Shared.Map;

namespace SS14.UnitTesting.Shared.Map
{
    [TestFixture, Parallelizable, TestOf(typeof(Tile))]
    class Tile_Tests
    {
        /// <summary>
        ///     A tile with an ID of zero is considered a special "empty" tile.
        ///     Empty tiles have no prototypes and are used in spacial cases all over the code.
        ///     Equivalent to an "Air" block in Minecraft.
        /// </summary>
        [Test]
        public void TileZeroIsSpecialEmpty()
        {
            var tile = new Tile(0);

            Assert.That(tile.IsEmpty, Is.True);
        }

        /// <summary>
        ///     Any tile with an index that isn't zero is not considered empty.
        /// </summary>
        [Test]
        public void NonZeroIdIsNotEmpty()
        {
            // there isn't a way to test all ints that are not zero
            // this test is more for completeness
            var tile = new Tile(1); 

            Assert.That(tile.IsEmpty, Is.False);
        }

        /// <summary>
        ///     A tile struct can losslessly be cast to a <c>UInt32</c> and back.
        /// </summary>
        [Test]
        public void ExplicitUintCasting()
        {
            var tile = new Tile(0x9999,0xAAAA);
            var value = (uint) tile;
            var newTile = (Tile) value;

            Assert.That(value, Is.EqualTo(0x9999AAAA));
            Assert.That(newTile.TileId, Is.EqualTo(0x9999));
            Assert.That(newTile.Data, Is.EqualTo(0xAAAA));
        }
    }
}
