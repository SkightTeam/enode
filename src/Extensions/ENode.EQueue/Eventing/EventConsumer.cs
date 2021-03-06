﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ECommon.IoC;
using ECommon.Serializing;
using ECommon.Socketing;
using ECommon.Utilities;
using ENode.Eventing;
using EQueue.Clients.Consumers;
using EQueue.Protocols;

namespace ENode.EQueue
{
    public class EventConsumer : IMessageHandler
    {
        private readonly Consumer _consumer;
        private readonly IBinarySerializer _binarySerializer;
        private readonly IEventTypeCodeProvider _eventTypeCodeProvider;
        private readonly IEventProcessor _eventProcessor;
        private readonly ConcurrentDictionary<Guid, IMessageContext> _messageContextDict;
        private readonly static ConsumerSetting _consumerSetting = new ConsumerSetting
        {
            MessageHandleMode = MessageHandleMode.Sequential
        };

        public Consumer Consumer { get { return _consumer; } }

        public EventConsumer()
            : this(_consumerSetting)
        {
        }
        public EventConsumer(string groupName)
            : this(_consumerSetting, groupName)
        {
        }
        public EventConsumer(ConsumerSetting setting)
            : this(setting, null)
        {
        }
        public EventConsumer(ConsumerSetting setting, string groupName)
            : this(setting, null, groupName)
        {
        }
        public EventConsumer(ConsumerSetting setting, string name, string groupName)
            : this(string.Format("{0}@{1}@{2}", SocketUtils.GetLocalIPV4(), string.IsNullOrEmpty(name) ? typeof(EventConsumer).Name : name, ObjectId.GenerateNewId()), setting, groupName)
        {
        }
        public EventConsumer(string id, ConsumerSetting setting, string groupName)
        {
            _consumer = new Consumer(id, setting, string.IsNullOrEmpty(groupName) ? typeof(EventConsumer).Name + "Group" : groupName, this);
            _binarySerializer = ObjectContainer.Resolve<IBinarySerializer>();
            _eventTypeCodeProvider = ObjectContainer.Resolve<IEventTypeCodeProvider>();
            _eventProcessor = ObjectContainer.Resolve<IEventProcessor>();
            _messageContextDict = new ConcurrentDictionary<Guid, IMessageContext>();
        }

        public EventConsumer Start()
        {
            _consumer.Start();
            return this;
        }
        public EventConsumer Subscribe(string topic)
        {
            _consumer.Subscribe(topic);
            return this;
        }
        public EventConsumer Shutdown()
        {
            _consumer.Shutdown();
            return this;
        }

        void IMessageHandler.Handle(QueueMessage message, IMessageContext context)
        {
            var eventStreamData = _binarySerializer.Deserialize(message.Body, typeof(EventStreamData)) as EventStreamData;
            var eventStream = ConvertToEventStream(eventStreamData);

            if (_messageContextDict.TryAdd(eventStream.CommandId, context))
            {
                _eventProcessor.Process(eventStream, new EventProcessContext(message, (processedEventStream, queueMessage) =>
                {
                    IMessageContext messageContext;
                    if (_messageContextDict.TryRemove(processedEventStream.CommandId, out messageContext))
                    {
                        messageContext.OnMessageHandled(queueMessage);
                    }
                }));
            }
        }

        private EventStream ConvertToEventStream(EventStreamData data)
        {
            var events = new List<IDomainEvent>();

            foreach (var typeData in data.Events)
            {
                var eventType = _eventTypeCodeProvider.GetType(typeData.TypeCode);
                var evnt = _binarySerializer.Deserialize(typeData.Data, eventType) as IDomainEvent;
                events.Add(evnt);
            }

            return new EventStream(data.CommandId, data.AggregateRootId, data.AggregateRootName, data.Version, data.Timestamp, events);
        }

        class EventProcessContext : IEventProcessContext
        {
            public Action<EventStream, QueueMessage> EventProcessedAction { get; private set; }
            public QueueMessage QueueMessage { get; private set; }

            public EventProcessContext(QueueMessage queueMessage, Action<EventStream, QueueMessage> eventProcessedAction)
            {
                QueueMessage = queueMessage;
                EventProcessedAction = eventProcessedAction;
            }

            public void OnEventProcessed(EventStream eventStream)
            {
                EventProcessedAction(eventStream, QueueMessage);
            }
        }
    }
}
