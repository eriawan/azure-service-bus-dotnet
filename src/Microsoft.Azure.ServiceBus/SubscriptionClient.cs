﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.ServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.ServiceBus.Primitives;

    public abstract class SubscriptionClient : ClientEntity
    {
        MessageReceiver innerReceiver;

        protected SubscriptionClient(ServiceBusConnection serviceBusConnection, string topicPath, string name, ReceiveMode receiveMode)
            : base($"{nameof(SubscriptionClient)}{ClientEntity.GetNextId()}({name})")
        {
            this.ServiceBusConnection = serviceBusConnection;
            this.TopicPath = topicPath;
            this.Name = name;
            this.SubscriptionPath = EntityNameHelper.FormatSubscriptionPath(this.TopicPath, this.Name);
            this.Mode = receiveMode;
        }

        public string TopicPath { get; private set; }

        public string Name { get; }

        public ReceiveMode Mode { get; private set; }

        public int PrefetchCount
        {
            get
            {
                return this.InnerReceiver.PrefetchCount;
            }

            set
            {
                this.InnerReceiver.PrefetchCount = value;
            }
        }

        internal string SubscriptionPath { get; private set; }

        internal MessageReceiver InnerReceiver
        {
            get
            {
                if (this.innerReceiver == null)
                {
                    lock (this.ThisLock)
                    {
                        if (this.innerReceiver == null)
                        {
                            this.innerReceiver = this.CreateMessageReceiver();
                        }
                    }
                }

                return this.innerReceiver;
            }
        }

        protected object ThisLock { get; } = new object();

        protected ServiceBusConnection ServiceBusConnection { get; }

        public static SubscriptionClient CreateFromConnectionString(string topicEntityConnectionString, string subscriptionName)
        {
            return CreateFromConnectionString(topicEntityConnectionString, subscriptionName, ReceiveMode.PeekLock);
        }

        public static SubscriptionClient CreateFromConnectionString(string topicEntityConnectionString, string subscriptionName, ReceiveMode mode)
        {
            if (string.IsNullOrWhiteSpace(topicEntityConnectionString))
            {
                throw Fx.Exception.ArgumentNullOrWhiteSpace(nameof(topicEntityConnectionString));
            }

            ServiceBusEntityConnection topicConnection = new ServiceBusEntityConnection(topicEntityConnectionString);
            return topicConnection.CreateSubscriptionClient(topicConnection.EntityPath, subscriptionName, mode);
        }

        public static SubscriptionClient Create(ServiceBusNamespaceConnection namespaceConnection, string topicPath, string subscriptionName)
        {
            return SubscriptionClient.Create(namespaceConnection, topicPath, subscriptionName, ReceiveMode.PeekLock);
        }

        public static SubscriptionClient Create(ServiceBusNamespaceConnection namespaceConnection, string topicPath, string subscriptionName, ReceiveMode mode)
        {
            if (namespaceConnection == null)
            {
                throw Fx.Exception.Argument(nameof(namespaceConnection), "Namespace Connection is null. Create a connection using the NamespaceConnection class");
            }

            if (string.IsNullOrWhiteSpace(topicPath))
            {
                throw Fx.Exception.Argument(nameof(namespaceConnection), "Topic Path is null");
            }

            return namespaceConnection.CreateSubscriptionClient(topicPath, subscriptionName, mode);
        }

        public static SubscriptionClient Create(ServiceBusEntityConnection topicConnection, string subscriptionName)
        {
            return SubscriptionClient.Create(topicConnection, subscriptionName, ReceiveMode.PeekLock);
        }

        public static SubscriptionClient Create(ServiceBusEntityConnection topicConnection, string subscriptionName, ReceiveMode mode)
        {
            if (topicConnection == null)
            {
                throw Fx.Exception.Argument(nameof(topicConnection), "Namespace Connection is null. Create a connection using the NamespaceConnection class");
            }

            return topicConnection.CreateSubscriptionClient(topicConnection.EntityPath, subscriptionName, mode);
        }

        public sealed override async Task CloseAsync()
        {
            await this.OnCloseAsync().ConfigureAwait(false);
        }

        public async Task<BrokeredMessage> ReceiveAsync()
        {
            IList<BrokeredMessage> messages = await this.ReceiveAsync(1).ConfigureAwait(false);
            if (messages != null && messages.Count > 0)
            {
                return messages[0];
            }

            return null;
        }

        public async Task<IList<BrokeredMessage>> ReceiveAsync(int maxMessageCount)
        {
            try
            {
                return await this.InnerReceiver.ReceiveAsync(maxMessageCount).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // TODO: Log Receive Exception
                throw;
            }
        }

        public async Task<BrokeredMessage> ReceiveBySequenceNumberAsync(long sequenceNumber)
        {
            IList<BrokeredMessage> messages = await this.ReceiveBySequenceNumberAsync(new long[] { sequenceNumber });
            if (messages != null && messages.Count > 0)
            {
                return messages[0];
            }

            return null;
        }

        public async Task<IList<BrokeredMessage>> ReceiveBySequenceNumberAsync(IEnumerable<long> sequenceNumbers)
        {
            try
            {
                return await this.InnerReceiver.ReceiveBySequenceNumberAsync(sequenceNumbers).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // TODO: Log Receive Exception
                throw;
            }
        }

        public Task CompleteAsync(Guid lockToken)
        {
            return this.CompleteAsync(new Guid[] { lockToken });
        }

        public async Task CompleteAsync(IEnumerable<Guid> lockTokens)
        {
            try
            {
                await this.InnerReceiver.CompleteAsync(lockTokens).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // TODO: Log Complete Exception
                throw;
            }
        }

        public Task AbandonAsync(Guid lockToken)
        {
            return this.AbandonAsync(new Guid[] { lockToken });
        }

        public async Task AbandonAsync(IEnumerable<Guid> lockTokens)
        {
            try
            {
                await this.InnerReceiver.AbandonAsync(lockTokens).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // TODO: Log Complete Exception
                throw;
            }
        }

        public Task<MessageSession> AcceptMessageSessionAsync()
        {
            return this.AcceptMessageSessionAsync(null);
        }

        public async Task<MessageSession> AcceptMessageSessionAsync(string sessionId)
        {
            MessageSession session = null;
            try
            {
                session = await this.OnAcceptMessageSessionAsync(sessionId).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // TODO: Log Complete Exception
                throw;
            }

            return session;
        }

        public Task DeferAsync(Guid lockToken)
        {
            return this.DeferAsync(new Guid[] { lockToken });
        }

        public async Task DeferAsync(IEnumerable<Guid> lockTokens)
        {
            try
            {
                await this.InnerReceiver.DeferAsync(lockTokens).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // TODO: Log Complete Exception
                throw;
            }
        }

        public Task DeadLetterAsync(Guid lockToken)
        {
            return this.DeadLetterAsync(new Guid[] { lockToken });
        }

        public async Task DeadLetterAsync(IEnumerable<Guid> lockTokens)
        {
            try
            {
                await this.InnerReceiver.DeadLetterAsync(lockTokens).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // TODO: Log Complete Exception
                throw;
            }
        }

        public async Task<DateTime> RenewMessageLockAsync(Guid lockToken)
        {
            try
            {
                return await this.InnerReceiver.RenewLockAsync(lockToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // TODO: Log Complete Exception
                throw;
            }
        }

        protected MessageReceiver CreateMessageReceiver()
        {
            return this.OnCreateMessageReceiver();
        }

        protected abstract MessageReceiver OnCreateMessageReceiver();

        protected abstract Task<MessageSession> OnAcceptMessageSessionAsync(string sessionId);

        protected abstract Task OnCloseAsync();
    }
}