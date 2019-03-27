using SS14.Shared.Prototypes;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.Map
{
    /// <summary>
    /// Contains a prototype of a tile definition.
    /// </summary>
    // Instantiated by the Prototype system through reflection.
    [Prototype("tile")]
    public sealed class PrototypeTileDefinition : TileDefinition, IPrototype
    {
        internal ushort FutureID { get; private set; }

        /// <inheritdoc />
        public void LoadFrom(YamlMappingNode mapping)
        {
            Name = mapping.GetNode("name").ToString();
            SpriteName = mapping.GetNode("texture").ToString();
            FutureID = (ushort)mapping.GetNode("id").AsInt();
        }
    }
}
