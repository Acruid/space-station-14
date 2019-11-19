using Robust.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public class AllEntityQuery
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
    public class PredicateEntityQuery
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
    ///     An entity query that will match all entities that intersect with the argument entity.
    /// </summary>
    [PublicAPI]
    public class IntersectingEntityQuery
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

        public bool TryMatch(IEntity entity)
        {
            if(Entity.TryGetComponent<ICollidableComponent>(out var collidable))
            {
                return collidable.MapID == entity.Transform.MapID && collidable.WorldAABB.Contains(entity.Transform.WorldPosition);
            }
            return false;
        }

        public IEnumerable<IEntity> EnumerateEntities(IEntityManager entityMan)
        {
            return entityMan.GetEntities().Where(TryMatch);
        }
    }

    /// <summary>
    ///     An entity query that will match one type of component.
    ///     This the fastest and most common query, and should be the default choice.
    /// </summary>
    public class TypeEntityQuery<T1>
        where T1 : IComponent
    {
        public bool TryMatch(IEntity entity, out T1 node)
        {
            node = default;

            if (!entity.TryGetComponent<T1>(out var item1))
                return false;

            node = item1;
            return true;
        }

        public IEnumerable<T1> EnumerateEntities(IEntityManager entityManager)
            => EnumerateEntities(entityManager.ComponentManager);

        public IEnumerable<T1> EnumerateEntities(IComponentManager componentManager)
        {
            return componentManager.GetAllComponents<T1>();
        }
    }

    /// <summary>
    ///     An entity query that will match two types of components.
    ///     This is an intersection of sets of components, put the rarest component first for optimal performance.
    /// </summary>
    public class TypeEntityQuery<T1, T2>
        where T1 : IComponent
        where T2 : IComponent
    {
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

        public IEnumerable<(T1, T2)> EnumerateEntities(IEntityManager entityManager)
            => EnumerateEntities(entityManager.ComponentManager);

        public IEnumerable<(T1, T2)> EnumerateEntities(IComponentManager componentManager)
        {
            foreach (var item1 in componentManager.GetAllComponents<T1>())
            {
                if (!componentManager.TryGetComponent<T2>(item1.Owner.Uid, out var item2))
                    continue;

                yield return (item1, item2);
            }
        }
    }

    /// <summary>
    ///     An entity query that will match three types of components.
    ///     This is an intersection of sets of components, put the rarest component first for optimal performance.
    /// </summary>
    public class TypeEntityQuery<T1, T2, T3>
        where T1 : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
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
            => EnumerateEntities(entityManager.ComponentManager);

        public IEnumerable<(T1, T2, T3)> EnumerateEntities(IComponentManager componentManager)
        {
            foreach (var item1 in componentManager.GetAllComponents<T1>())
            {
                var uid = item1.Owner.Uid;
                if (!componentManager.TryGetComponent<T2>(uid, out var item2))
                    continue;

                if (!componentManager.TryGetComponent<T3>(uid, out var item3))
                    continue;

                yield return (item1, item2, item3);
            }
        }
    }
}
