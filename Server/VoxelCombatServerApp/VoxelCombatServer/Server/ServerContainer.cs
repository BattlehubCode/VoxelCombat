using log4net;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Configuration;

namespace Battlehub.VoxelCombat
{
    public abstract class ServerContainer : ITimeService
    {
        public static readonly Guid ServerIdentity = new Guid(ConfigurationManager.AppSettings["ServerIdentity"]);

        private class QueuedMessage
        {
            public ILowProtocol Sender;
            public byte[] Message;
            public LowRequestArgs Request;

            public bool IsRequest
            {
                get { return Request != null; }
            }

            public QueuedMessage(ILowProtocol sender, byte[] message)
            {
                Message = message;
                Sender = sender;
            }

            public QueuedMessage(ILowProtocol sender, LowRequestArgs requestArgs)
            {
                Request = requestArgs;
                Sender = sender;
            }
        }

        public int ConnectionsCount
        {
            get
            {
                int count;
                lock(m_protocolToClientId)
                {
                    count = m_protocolToClientId.Count;
                }
                return count;
            }
        }

        protected readonly ILog Log;

        private readonly Queue<QueuedMessage> m_incomingMessages = new Queue<QueuedMessage>();
        private readonly Queue<QueuedMessage> m_outgoingMessages = new Queue<QueuedMessage>();

        private readonly Dictionary<ILowProtocol, Guid> m_protocolToClientId = new Dictionary<ILowProtocol, Guid>();
        private readonly Dictionary<Guid, ILowProtocol> m_clientIdToProtocol = new Dictionary<Guid, ILowProtocol>();
   
        private bool m_isMainThreadRunning;
        private bool m_isSecondaryThreadRunning;

        private Stopwatch m_stopwatch;
        public float Time
        {
            get { return m_stopwatch.ElapsedMilliseconds / 1000.0f; }
        }

        protected ServerContainer()
        {
            Log = LogManager.GetLogger(GetType());
        }

        protected ILowProtocol GetSender(Guid clientId)
        {
            ILowProtocol result;
            if (m_clientIdToProtocol.TryGetValue(clientId, out result))
            {
                return result;
            }
            return null;
        }

        public void Run()
        {
            if (m_isMainThreadRunning)
            {
                throw new InvalidOperationException("Main thread is running");
            }

            if (m_isSecondaryThreadRunning)
            {
                throw new InvalidOperationException("Secondary thread is running");
            }

            lock (m_incomingMessages)
            {
                if (m_incomingMessages.Count > 0)
                {
                    throw new InvalidOperationException("Incoming messages queue is not empty");
                }

                m_isMainThreadRunning = true;
            }

            lock (m_outgoingMessages)
            {
                if (m_outgoingMessages.Count > 0)
                {
                    throw new InvalidOperationException("Outgoing messages queue is not empty");
                }

                m_isSecondaryThreadRunning = true;
            }

            Thread mainThread = new Thread(MainThread);
            mainThread.Start();

            Thread secondaryThread = new Thread(SecondaryThread);
            secondaryThread.Start();
        }

        protected virtual void OnBeforeRun()
        {

        }

        public void Stop()
        {
            lock (m_incomingMessages)
            {
                m_isMainThreadRunning = false;
                Monitor.PulseAll(m_incomingMessages);
            }

            lock (m_outgoingMessages)
            {
                m_isSecondaryThreadRunning = false;
                Monitor.PulseAll(m_outgoingMessages);
            }
        }

        protected virtual void OnAfterStop()
        {

        }

        public void RegisterConnection(ILowProtocol client)
        {
            lock(m_protocolToClientId)
            {
                lock (m_incomingMessages)
                {
                    Log.Info("Register Connection");

                    m_protocolToClientId.Add(client, Guid.Empty);
                    client.Message += OnIncomingMessage;
                    client.Request += OnIncomingRequest;
                }
            }
        }


        protected void RegisterClient(ILowProtocol protocol, Guid clientId)
        {
            lock(m_protocolToClientId)
            {
                lock (m_incomingMessages)
                {
                    if (m_protocolToClientId.ContainsKey(protocol))
                    {
                        Log.Info("Register Client " + clientId);

                        m_protocolToClientId[protocol] = clientId;
                        m_clientIdToProtocol.Add(clientId, protocol);
                        OnRegisterClientSafe(protocol, clientId);
                    }
                }
            }
            
        }

        protected virtual void OnRegisterClientSafe(ILowProtocol protocol, Guid clientId)
        {

        }

        protected virtual void OnUnregisterClientSafe(ILowProtocol protocol, Guid clientId)
        {

        }

        public void UnregisterConnection(ILowProtocol client)
        {
            lock (m_protocolToClientId)
            {
                lock (m_incomingMessages)
                {
                    if (m_protocolToClientId.ContainsKey(client))
                    {
                        Guid guid = m_protocolToClientId[client];

                        m_clientIdToProtocol.Remove(guid);
                        m_protocolToClientId.Remove(client);

                        if (guid != Guid.Empty)
                        {
                            OnUnregisterClientSafe(client, guid);
                        }

                        Log.Info("Unregister Connection for client " + guid);

                        client.Message -= OnIncomingMessage;
                        client.Request -= OnIncomingRequest;
                    }
                }
            }
        }

        private void OnIncomingRequest(ILowProtocol sender, LowRequestArgs args)
        {
            lock (m_incomingMessages)
            {
                if (m_incomingMessages.Count == 0)
                {
                    Monitor.PulseAll(m_incomingMessages);
                }

                m_incomingMessages.Enqueue(new QueuedMessage(sender, args));
            }
        }

        private void OnIncomingMessage(ILowProtocol sender, byte[] data)
        {
            lock (m_incomingMessages)
            {
                if (m_incomingMessages.Count == 0)
                {
                    Monitor.PulseAll(m_incomingMessages);
                }

                m_incomingMessages.Enqueue(new QueuedMessage(sender, data));
            }
        }

        private void MainThread()
        {
            m_stopwatch = Stopwatch.StartNew();
            OnBeforeRun();

            while (true)
            {
                QueuedMessage message = null;
                lock (m_incomingMessages)
                {
                    if (!m_isMainThreadRunning)
                    {
                        m_incomingMessages.Clear();
                        m_stopwatch.Stop();
                        OnAfterStop();
                        break;
                    }

                    //if (m_incomingMessages.Count == 0)
                    //{
                    //    /*The thread that currently owns the lock on the specified object invokes this method in order to release the object so that another thread can access it. The caller is blocked while waiting to reacquire the lock. This method is called when the caller needs to wait for a state change that will occur as a result of another thread's operations.*/
                    //    Monitor.Wait(m_incomingMessages);
                    //    continue;
                    //}
                    if(m_incomingMessages.Count > 0)
                    {
                        message = m_incomingMessages.Dequeue(); 
                    }
                }

                try
                {
                    if(message != null)
                    {
                        if (message.IsRequest)
                        {
                            OnRequest(message.Sender, message.Request);
                        }
                        else
                        {
                            OnMessage(message.Sender, message.Message);
                        }
                    }
                  
                    OnTick();
                }
                catch (Exception e)
                {
                    Log.Error(e.Message, e);
#if DEBUG
                    throw;
#endif
                }
            }
        }

        protected virtual void OnTick()
        {
        
        }

        protected abstract void OnRequest(ILowProtocol sender, LowRequestArgs request);

        protected abstract void OnMessage(ILowProtocol sender, byte[] message);


        protected void Send(RemoteEvent.Evt evt, Error error, Guid target, params RemoteArg[] args)
        {
            RemoteEvent remoteEvent = new RemoteEvent(evt, error, args);
            byte[] data = ProtobufSerializer.Serialize(remoteEvent);

            lock (m_protocolToClientId)
            {
                lock (m_outgoingMessages)
                {
                    Guid clientId = target;
                    ILowProtocol protocol;

                    if (m_clientIdToProtocol.TryGetValue(clientId, out protocol))
                    {
                        EnqueueSend(protocol, data);
                    }
                }
            }
        }

        public void Broadcast(RemoteEvent.Evt evt, Error error, ServerEventArgs globalArgs, params RemoteArg[] args)
        {
            RemoteEvent remoteEvent = new RemoteEvent(evt, error, args);
            byte[] result = ProtobufSerializer.Serialize(remoteEvent);
            if (globalArgs.Targets != null && globalArgs.Targets.Length > 0)
            {
                Broadcast(globalArgs.Targets, result);
            }
            else 
            {
                BroadcastAllExcept(globalArgs.Except, result);
            }
        }

        protected void Broadcast(Guid[] targets, byte[] data)
        {
            lock (m_protocolToClientId)
            {
                lock (m_outgoingMessages)
                {
                    for (int i = 0; i < targets.Length; ++i)
                    {
                        Guid clientId = targets[i];
                        ILowProtocol protocol;

                        if (m_clientIdToProtocol.TryGetValue(clientId, out protocol))
                        {
                            EnqueueSend(protocol, data);
                        }
                    }
                }
            }
        }

        protected void BroadcastAllExcept(Guid except, byte[] data)
        {
            lock (m_protocolToClientId)
            {
                lock (m_outgoingMessages)
                {
                    foreach (KeyValuePair<Guid, ILowProtocol> kvp in m_clientIdToProtocol)
                    {
                        if (kvp.Key == except)
                        {
                            continue;
                        }

                        ILowProtocol protocol = kvp.Value;
                        EnqueueSend(protocol, data);
                    }
                }
            }
        }

        protected void BroadcastAll(byte[] data)
        {
            lock (m_protocolToClientId)
            {
                lock (m_outgoingMessages)
                {
                    foreach (KeyValuePair<Guid, ILowProtocol> kvp in m_clientIdToProtocol)
                    { 
                        ILowProtocol protocol = kvp.Value;
                        EnqueueSend(protocol, data);
                    }
                }
            }
        }

        private void EnqueueSend(ILowProtocol sender, byte[] data)
        {
            QueuedMessage message = new QueuedMessage(sender, data);
            if (m_outgoingMessages.Count == 0)
            {
                Monitor.PulseAll(m_outgoingMessages);
            }
            m_outgoingMessages.Enqueue(message);
        }


        protected void Return(ILowProtocol sender, LowRequestArgs request, Error error, params RemoteArg[] args)
        {
            RemoteResult remoteResult = new RemoteResult(error, args);
            byte[] result = ProtobufSerializer.Serialize(remoteResult);
            EnqueueResponse(sender, request.Id, result);
        }

        protected void EnqueueResponse(ILowProtocol sender, int requestId, byte[] data)
        {
            QueuedMessage message = new QueuedMessage(sender, new LowRequestArgs(requestId, data));
            lock (m_outgoingMessages)
            {
                if (m_outgoingMessages.Count == 0)
                {
                    Monitor.PulseAll(m_outgoingMessages);
                }
                m_outgoingMessages.Enqueue(message);
            }
        }

        private void SecondaryThread()
        {
            while (true)
            {
                QueuedMessage message;
                lock (m_outgoingMessages)
                {
                    if (!m_isSecondaryThreadRunning)
                    {
                        m_outgoingMessages.Clear();
                        break;
                    }

                    if (m_outgoingMessages.Count == 0)
                    {
                        /*The thread that currently owns the lock on the specified object invokes this method in order to release the object so that another thread can access it. The caller is blocked while waiting to reacquire the lock. This method is called when the caller needs to wait for a state change that will occur as a result of another thread's operations.*/
                        Monitor.Wait(m_outgoingMessages);
                        continue;
                    }

                    message = m_outgoingMessages.Dequeue();
                }

                try
                {
                    if(message.IsRequest)
                    {
                        message.Sender.Response(message.Request.Id, message.Request.Data, done =>
                        {
                            if (!done)
                            {
                                Log.Warn("Send message failed");
                            }
                        });
                    }
                    else
                    {
                        message.Sender.Send(message.Message, done =>
                        {
                            if (!done)
                            {
                                Log.Warn("Send message failed");
                            }
                        });
                    }
                  
                }
                catch (Exception e)
                {
                    Log.Error(e.Message, e);
#if DEBUG
                    throw;
#endif
                }
            }
        }
    }
}
