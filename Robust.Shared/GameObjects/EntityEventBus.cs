﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// Provides a central event bus that EntitySystems can subscribe to. This is the main way that
    /// EntitySystems communicate with each other.
    /// </summary>
    [PublicAPI]
    public interface IEventBus
    {
        /// <summary>
        /// Subscribes an event handler for a event type.
        /// </summary>
        /// <typeparam name="T">Event type to subscribe to.</typeparam>
        /// <param name="eventHandler">Delegate that handles the event.</param>
        /// <param name="subscriber">Subscriber that owns the handler.</param>
        void SubscribeEvent<T>(EntityEventHandler<T> eventHandler, IEntityEventSubscriber subscriber)
            where T : EntityEventArgs;

        /// <summary>
        /// Unsubscribes all event handlers of a given type.
        /// </summary>
        /// <typeparam name="T">Event type being unsubscribed from.</typeparam>
        /// <param name="subscriber">Subscriber that owns the handlers.</param>
        void UnsubscribeEvent<T>(IEntityEventSubscriber subscriber)
            where T : EntityEventArgs;

        /// <summary>
        /// Immediately raises an event onto the bus.
        /// </summary>
        /// <param name="sender">Object that raised the event.</param>
        /// <param name="toRaise">Event being raised.</param>
        void RaiseEvent(object sender, EntityEventArgs toRaise);

        /// <summary>
        /// Queues an event to be raised at a later time.
        /// </summary>
        /// <param name="sender">Object that raised the event.</param>
        /// <param name="toRaise">Event being raised.</param>
        void QueueEvent(object sender, EntityEventArgs toRaise);

        /// <summary>
        /// Waits for an event to be raised. You do not have to subscribe to the event.
        /// </summary>
        /// <typeparam name="T">Event type being waited for.</typeparam>
        /// <returns></returns>
        Task<T> AwaitEvent<T>()
            where T : EntityEventArgs;

        /// <summary>
        /// Waits for an event to be raised. You do not have to subscribe to the event.
        /// </summary>
        /// <typeparam name="T">Event type being waited for.</typeparam>
        /// <returns></returns>
        Task<T> AwaitEvent<T>(CancellationToken cancellationToken)
            where T : EntityEventArgs;

        /// <summary>
        /// Unsubscribes all event handlers for a given subscriber.
        /// </summary>
        /// <param name="subscriber">Owner of the handlers being removed.</param>
        void UnsubscribeEvents(IEntityEventSubscriber subscriber);
    }

    /// <inheritdoc />
    internal interface IEntityEventBus : IEventBus
    {
        /// <summary>
        /// Raises all queued events onto the event bus. This needs to be called often.
        /// </summary>
        void ProcessEventQueue();
    }

    /// <inheritdoc />
    internal class EntityEventBus : IEntityEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _eventSubscriptions
            = new Dictionary<Type, List<Delegate>>();

        private readonly Dictionary<IEntityEventSubscriber, Dictionary<Type, Delegate>> _inverseEventSubscriptions
            = new Dictionary<IEntityEventSubscriber, Dictionary<Type, Delegate>>();

        private readonly Queue<(object sender, EntityEventArgs eventArgs)> _eventQueue
            = new Queue<(object, EntityEventArgs)>();

        private readonly Dictionary<Type, (CancellationTokenRegistration, TaskCompletionSource<EntityEventArgs>)>
            _awaitingMessages
                = new Dictionary<Type, (CancellationTokenRegistration, TaskCompletionSource<EntityEventArgs>)>();

        /// <inheritdoc />
        public void UnsubscribeEvents(IEntityEventSubscriber subscriber)
        {
            if (!_inverseEventSubscriptions.TryGetValue(subscriber, out var val))
                return;

            // UnsubscribeEvent modifies _inverseEventSubscriptions, requires val to be cached
            foreach (var (type, @delegate) in val.ToList())
            {
                UnsubscribeEvent(type, @delegate, subscriber);
            }
        }

        /// <inheritdoc />
        public void ProcessEventQueue()
        {
            while (_eventQueue.Count != 0)
            {
                ProcessSingleEvent(_eventQueue.Dequeue());
            }
        }

        /// <inheritdoc />
        public void SubscribeEvent<T>(EntityEventHandler<T> eventHandler, IEntityEventSubscriber subscriber)
            where T : EntityEventArgs
        {
            if (eventHandler == null)
                throw new ArgumentNullException(nameof(eventHandler));

            if(subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            var eventType = typeof(T);
            if (!_eventSubscriptions.TryGetValue(eventType, out var subscriptions))
                _eventSubscriptions.Add(eventType, new List<Delegate> {eventHandler});
            else if (!subscriptions.Contains(eventHandler))
                subscriptions.Add(eventHandler);

            if (!_inverseEventSubscriptions.TryGetValue(subscriber, out var inverseSubscription))
            {
                inverseSubscription = new Dictionary<Type, Delegate>
                {
                    {eventType, eventHandler}
                };

                _inverseEventSubscriptions.Add(
                    subscriber,
                    inverseSubscription
                );
            }

            else if (!inverseSubscription.ContainsKey(eventType))
            {
                inverseSubscription.Add(eventType, eventHandler);
            }
        }

        /// <inheritdoc />
        public void UnsubscribeEvent<T>(IEntityEventSubscriber subscriber)
            where T : EntityEventArgs
        {
            var eventType = typeof(T);

            if (_inverseEventSubscriptions.TryGetValue(subscriber, out var inverse)
                && inverse.TryGetValue(eventType, out var @delegate))
                UnsubscribeEvent(eventType, @delegate, subscriber);
        }

        /// <inheritdoc />
        public void RaiseEvent(object sender, EntityEventArgs toRaise)
        {
            if(toRaise == null)
                throw new ArgumentNullException(nameof(toRaise));

            ProcessSingleEvent((sender, toRaise));
        }

        /// <inheritdoc />
        public void QueueEvent(object sender, EntityEventArgs toRaise)
        {
            if(toRaise == null)
                throw new ArgumentNullException(nameof(toRaise));

            _eventQueue.Enqueue((sender, toRaise));
        }

        /// <inheritdoc />
        public Task<T> AwaitEvent<T>()
            where T : EntityEventArgs
        {
            return AwaitEvent<T>(default);
        }

        /// <inheritdoc />
        public Task<T> AwaitEvent<T>(CancellationToken cancellationToken)
            where T : EntityEventArgs
        {
            var type = typeof(T);
            if (_awaitingMessages.ContainsKey(type))
            {
                throw new InvalidOperationException("Cannot await the same message type twice at once.");
            }

            var tcs = new TaskCompletionSource<EntityEventArgs>();
            CancellationTokenRegistration reg = default;
            if (cancellationToken != default)
            {
                reg = cancellationToken.Register(() =>
                {
                    _awaitingMessages.Remove(type);
                    tcs.TrySetCanceled();
                });
            }

            // Tiny trick so we can return T while the tcs is passed an EntitySystemMessage.
            async Task<T> DoCast(Task<EntityEventArgs> task)
            {
                return (T)await task;
            }

            _awaitingMessages.Add(type, (reg, tcs));
            return DoCast(tcs.Task);
        }

        private void UnsubscribeEvent(Type eventType, Delegate evh, IEntityEventSubscriber s)
        {
            if (_eventSubscriptions.TryGetValue(eventType, out var subscriptions) && subscriptions.Contains(evh))
                subscriptions.Remove(evh);

            if (_inverseEventSubscriptions.TryGetValue(s, out var inverse) && inverse.ContainsKey(eventType))
                inverse.Remove(eventType);
        }

        private void ProcessSingleEvent((object sender, EntityEventArgs eventArgs) argsTuple)
        {
            var (sender, eventArgs) = argsTuple;
            var eventType = eventArgs.GetType();

            if (_eventSubscriptions.TryGetValue(eventType, out var subs))
            {
                foreach (var handler in subs)
                {
                    handler.DynamicInvoke(sender, eventArgs);
                }
            }

            if (_awaitingMessages.TryGetValue(eventType, out var awaiting))
            {
                var (_, tcs) = awaiting;
                tcs.TrySetResult(eventArgs);
                _awaitingMessages.Remove(eventType);
            }
        }
    }
}
