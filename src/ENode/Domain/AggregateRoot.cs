﻿using System;
using System.Collections.Generic;
using ENode.Eventing;
using ENode.Infrastructure;

namespace ENode.Domain
{
    /// <summary>Aggregate root base class.
    /// </summary>
    /// <typeparam name="TAggregateRootId"></typeparam>
    [Serializable]
    public abstract class AggregateRoot<TAggregateRootId> : IAggregateRoot
    {
        private Queue<IDomainEvent> _uncommittedEvents;
        private long _version;
        private TAggregateRootId _id;

        /// <summary>The strong type unique id of aggregate root.
        /// </summary>
        public TAggregateRootId Id
        {
            get
            {
                return _id;
            }
            protected set
            {
                _id = value;
            }
        }

        /// <summary>Default constructor.
        /// </summary>
        protected AggregateRoot()
        {
            Initialize();
        }

        /// <summary>Raise a domain event. The domain event will be put into the local uncommitted event queue.
        /// </summary>
        /// <param name="evnt"></param>
        protected void RaiseEvent(IDomainEvent evnt)
        {
            _uncommittedEvents.Enqueue(evnt);
        }

        /// <summary>The unique id of aggregate root.
        /// </summary>
        string IAggregateRoot.UniqueId
        {
            get
            {
                return _id.ToString();
            }
        }
        /// <summary>The version of aggregate root.
        /// </summary>
        long IAggregateRoot.Version
        {
            get
            {
                return _version;
            }
        }
        /// <summary>Returns all the uncommitted domain events of the current aggregate root.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IDomainEvent> IAggregateRoot.GetUncommittedEvents()
        {
            return _uncommittedEvents;
        }
        /// <summary>Clear all the uncommitted domain events of the current aggregate root.
        /// </summary>
        void IAggregateRoot.ClearUncommittedEvents()
        {
            _uncommittedEvents.Clear();
        }

        /// <summary>Initialize the aggregate root.
        /// <remarks>This method must be provided as enode will call it when rebuilding the aggregate using event sourcing.
        /// </remarks>
        /// </summary>
        protected virtual void Initialize()
        {
            _uncommittedEvents = new Queue<IDomainEvent>();
        }
        /// <summary>Increase the version of aggregate root.
        /// <remarks>This method must be provided as enode will call it when rebuilding the aggregate using event sourcing.
        /// </remarks>
        /// </summary>
        private void IncreaseVersion()
        {
            _version++;
        }
    }
}
