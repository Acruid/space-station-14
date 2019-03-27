using System;
using System.Linq;
using System.Collections.Generic;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Prototypes;

namespace SS14.Shared.Map
{
    /// <summary>
    ///     This class manages a collection of <see cref="MapManager"/> tile definitions.
    /// </summary>
    internal class TileDefinitionManager : ITileDefinitionManager
    {
        [Dependency]
        private readonly IPrototypeManager PrototypeManager;

        protected readonly List<ITileDefinition> TileDefs;
        private readonly Dictionary<string, ITileDefinition> _tileNames;
        private readonly Dictionary<ITileDefinition, ushort> _tileIds;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public TileDefinitionManager()
        {
            TileDefs = new List<ITileDefinition>();
            _tileNames = new Dictionary<string, ITileDefinition>();
            _tileIds = new Dictionary<ITileDefinition, ushort>();
        }

        /// <inheritdoc />
        public virtual void Initialize()
        {
            foreach (var prototype in PrototypeManager.EnumeratePrototypes<PrototypeTileDefinition>().OrderBy(p => p.FutureId))
            {
                prototype.Register(this);
            }
        }

        /// <inheritdoc />
        public virtual ushort Register(ITileDefinition tileDef)
        {
            if (_tileIds.TryGetValue(tileDef, out ushort id))
            {
                throw new InvalidOperationException($"TileDefinition is already registered: {tileDef.GetType()}, id: {id}");
            }

            var name = tileDef.Name;
            if (_tileNames.ContainsKey(name))
            {
                throw new ArgumentException("Another tile definition with the same name has already been registered.", nameof(tileDef));
            }

            id = checked((ushort) TileDefs.Count);
            TileDefs.Add(tileDef);
            _tileNames[name] = tileDef;
            _tileIds[tileDef] = id;
            return id;
        }

        /// <inheritdoc />
        public ITileDefinition this[string name] => _tileNames[name];

        /// <inheritdoc />
        public ITileDefinition this[int id] => TileDefs[id];

        /// <inheritdoc />
        public int Count => TileDefs.Count;

        /// <inheritdoc />
        public IEnumerator<ITileDefinition> GetEnumerator()
        {
            return TileDefs.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
