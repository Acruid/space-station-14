using Robust.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     An entity query that will let all entities pass.
    ///     This is the same as matching <c>ITransformComponent</c>, but faster.
    /// </summary>
    [PublicAPI]
    public class AllEntityQuery : IEntityQuery
    {
        /// <inheritdoc />
        public bool Match(IEntity entity) => true;

        /// <inheritdoc />
        public IEnumerable<IEntity> Match(IEntityManager entityMan)
        {
            return entityMan.GetEntities();
        }
    }

    /// <summary>
    ///     An entity query which will match entities based on a predicate.
    ///     If you only want a single type of Component, use <c>TypeEntityQuery</c>.
    /// </summary>
    [PublicAPI]
    public class PredicateEntityQuery : IEntityQuery
    {
        private readonly Predicate<IEntity> Predicate;

        /// <summary>
        ///     Constructs a new instance of <c>PredicateEntityQuery</c>.
        /// </summary>
        /// <param name="predicate"></param>
        public PredicateEntityQuery(Predicate<IEntity> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            Predicate = predicate;
        }

        /// <inheritdoc />
        public bool Match(IEntity entity) => Predicate(entity);

        /// <inheritdoc />
        public IEnumerable<IEntity> Match(IEntityManager entityMan)
        {
            return entityMan.GetEntities().Where(entity => Predicate(entity));
        }
    }

    /// <summary>
    ///     An entity query that will match one type of component.
    ///     This the fastest and most common query, and should be the default choice.
    /// </summary>
    [PublicAPI]
    public class TypeEntityQuery : IEntityQuery
    {
        private readonly Type ComponentType;

        /// <summary>
        ///     Constructs a new instance of <c>TypeEntityQuery</c>.
        /// </summary>
        /// <param name="componentType">Type of the component to match.</param>
        public TypeEntityQuery(Type componentType)
        {
            DebugTools.Assert(typeof(IComponent).IsAssignableFrom(componentType), "componentType must inherit IComponent");

            ComponentType = componentType;
        }

        /// <inheritdoc />
        public bool Match(IEntity entity) => entity.HasComponent(ComponentType);

        /// <inheritdoc />
        public IEnumerable<IEntity> Match(IEntityManager entityMan)
        {
            return entityMan.ComponentManager.GetAllComponents(ComponentType).Select(component => component.Owner);
        }
    }

    /// <summary>
    ///     An entity query that will match one type of component.
    ///     This the fastest and most common query, and should be the default choice.
    /// </summary>
    /// <typeparamref name="T">Type of component to match.</typeparamref>
    [PublicAPI]
    public class TypeEntityQuery<T> : IEntityQuery where T : IComponent
    {
        public bool Match(IEntity entity) => entity.HasComponent<T>();

        public IEnumerable<IEntity> Match(IEntityManager entityMan)
        {
            return entityMan.ComponentManager.GetAllComponents<T>().Select(component => component.Owner);
        }
    }

    /// <summary>
    ///     An entity query that will match all entities that intersect with the argument entity.
    /// </summary>
    [PublicAPI]
    public class IntersectingEntityQuery : IEntityQuery
    {
        private readonly IEntity Entity;

        /// <summary>
        ///     Constructs a new instance of <c>TypeEntityQuery</c>.
        /// </summary>
        /// <param name="componentType">Type of the component to match.</param>
        public IntersectingEntityQuery(IEntity entity)
        {
            Entity = entity;
        }

        /// <inheritdoc />
        public bool Match(IEntity entity)
        {
            if(Entity.TryGetComponent<ICollidableComponent>(out var collidable))
            {
                return collidable.MapID == entity.Transform.MapID && collidable.WorldAABB.Contains(entity.Transform.WorldPosition);
            }
            return false;
        }

        public IEnumerable<IEntity> Match(IEntityManager entityMan)
        {
            return entityMan.GetEntities().Where(entity => Match(entity));
        }
    }

    public abstract class NodeEntityQuery<T> : IEntityQuery
        where T : struct
    {
        public bool Match(IEntity entity)
        {
            return TryMatch(entity, out _);
        }

        public IEnumerable<IEntity> Match(IEntityManager entityMan)
        {
            return entityMan.GetEntities().Where(entity => TryMatch(entity, out _));
        }

        public abstract bool TryMatch(IEntity entity, out T node);
    }

    public class TypeEntityQuery<T1, T2> : IEntityQuery
        where T1 : class
        where T2 : class
    {
        public bool Match(IEntity entity)
        {
            return TryMatch(entity, out _);
        }

        public IEnumerable<IEntity> Match(IEntityManager entityMan)
        {
            return entityMan.GetEntities().Where(entity => TryMatch(entity, out _));
        }

        public bool TryMatch(IEntity entity, out (T1, T2) node)
        {
            node = default;

            if (!entity.TryGetComponent<T1>(out var item1))
                return false;

            if (!entity.TryGetComponent<T2>(out var item2))
                return false;

            node = (item1, item2);
            return true;
        }
    }

    public class TypeEntityQuery<T1, T2, T3> : IEntityQuery
        where T1 : class
        where T2 : class
        where T3 : class
    {
        public bool Match(IEntity entity)
        {
            return TryMatch(entity, out _);
        }

        public IEnumerable<IEntity> Match(IEntityManager entityMan)
        {
            return entityMan.GetEntities().Where(entity => TryMatch(entity, out _));
        }

        public bool TryMatch(IEntity entity, out (T1, T2, T3) node)
        {
            node = default;

            if (!entity.TryGetComponent<T1>(out var item1))
                return false;

            if (!entity.TryGetComponent<T2>(out var item2))
                return false;

            if (!entity.TryGetComponent<T3>(out var item3))
                return false;

            node = (item1, item2, item3);
            return true;
        }

        public IEnumerable<(T1, T2, T3)> EnumerateEntities(IEntityManager entityManager)
        {
            foreach (var entity in entityManager.GetEntities())
            {
                if (!TryMatch(entity, out var tuple))
                    continue;

                yield return tuple;
            }
        }
    }
}
