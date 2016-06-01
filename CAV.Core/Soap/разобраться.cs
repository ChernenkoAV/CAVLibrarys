//using System;
//using System.Collections.Generic;
//using System.ServiceModel;
//using System.ServiceModel.Channels;
//using System.ServiceModel.Dispatcher;
//using System.Threading;

//namespace WcfExtensibility
//{
//    public class ClientMessageInspectorBindingElement : BindingElement
//    {
//        List<IClientMessageInspector> messageInspectors = new List<IClientMessageInspector>();

//        public ClientMessageInspectorBindingElement()
//        {
//        }

//        public List<IClientMessageInspector> MessageInspectors
//        {
//            get
//            {
//                return this.messageInspectors;
//            }
//        }

//        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
//        {
//            return typeof(TChannel) == typeof(IRequestChannel) &&
//                context.CanBuildInnerChannelFactory<TChannel>();
//        }

//        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
//        {
//            if (context == null)
//            {
//                throw new ArgumentNullException("context");
//            }

//            if (typeof(TChannel) != typeof(IRequestChannel))
//            {
//                throw new InvalidOperationException("Invalid channel shape");
//            }

//            if (this.MessageInspectors.Count == 0)
//            {
//                return base.BuildChannelFactory<TChannel>(context);
//            }
//            else
//            {
//                ClientMessageInspectorChannelFactory factory = new ClientMessageInspectorChannelFactory(
//                    context.BuildInnerChannelFactory<IRequestChannel>(),
//                    this.MessageInspectors);

//                return (IChannelFactory<TChannel>)factory;
//            }
//        }

//        public override BindingElement Clone()
//        {
//            return new ClientMessageInspectorBindingElement { messageInspectors = new List<IClientMessageInspector>(this.messageInspectors) };
//        }

//        public override T GetProperty<T>(BindingContext context)
//        {
//            return context.GetInnerProperty<T>();
//        }

//        class ClientMessageInspectorChannelFactory : ChannelFactoryBase<IRequestChannel>
//        {
//            private IChannelFactory<IRequestChannel> innerFactory;
//            private List<IClientMessageInspector> messageInspectors;

//            public ClientMessageInspectorChannelFactory(IChannelFactory<IRequestChannel> innerFactory, List<IClientMessageInspector> messageInspectors)
//            {
//                this.innerFactory = innerFactory;
//                this.messageInspectors = messageInspectors;
//            }

//            protected override IRequestChannel OnCreateChannel(EndpointAddress address, Uri via)
//            {
//                IRequestChannel innerChannel = this.innerFactory.CreateChannel(address, via);
//                ClientMessageInspectorChannel clientChannel = new ClientMessageInspectorChannel(this, innerChannel, this.messageInspectors);
//                return clientChannel;
//            }

//            #region Methods which simply delegate to the inner factory
//            protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
//            {
//                return this.innerFactory.BeginOpen(timeout, callback, state);
//            }

//            protected override void OnEndOpen(IAsyncResult result)
//            {
//                this.innerFactory.EndOpen(result);
//            }

//            protected override void OnOpen(TimeSpan timeout)
//            {
//                this.innerFactory.Open(timeout);
//            }

//            protected override void OnAbort()
//            {
//                this.innerFactory.Abort();
//            }

//            public override T GetProperty<T>()
//            {
//                return this.innerFactory.GetProperty<T>();
//            }

//            protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
//            {
//                return this.innerFactory.BeginClose(timeout, callback, state);
//            }

//            protected override void OnClose(TimeSpan timeout)
//            {
//                this.innerFactory.Close(timeout);
//            }

//            protected override void OnEndClose(IAsyncResult result)
//            {
//                this.innerFactory.EndClose(result);
//            }
//            #endregion
//        }

//        class ClientMessageInspectorChannel : ChannelBase, IRequestChannel
//        {
//            private IRequestChannel innerChannel;
//            private List<IClientMessageInspector> messageInspectors;

//            public ClientMessageInspectorChannel(ChannelManagerBase channelManager, IRequestChannel innerChannel, List<IClientMessageInspector> messageInspectors)
//                : base(channelManager)
//            {
//                this.innerChannel = innerChannel;
//                this.messageInspectors = messageInspectors;
//            }

//            public IAsyncResult BeginRequest(Message message, TimeSpan timeout, AsyncCallback callback, object state)
//            {
//                object[] correlationStates = new object[this.messageInspectors.Count];
//                for (int i = 0; i < this.messageInspectors.Count; i++)
//                {
//                    correlationStates[i] = this.messageInspectors[i].BeforeSendRequest(ref message, null);
//                }

//                CorrelationStateAsyncResult.State newState = new CorrelationStateAsyncResult.State
//                {
//                    OriginalAsyncCallback = callback,
//                    ExtensibilityCorrelationState = correlationStates,
//                    OriginalAsyncState = state,
//                };
//                IAsyncResult originalResult = this.innerChannel.BeginRequest(message, timeout, this.BeginRequestCallback, newState);
//                return new CorrelationStateAsyncResult(originalResult, state, correlationStates);
//            }

//            void BeginRequestCallback(IAsyncResult asyncResult)
//            {
//                CorrelationStateAsyncResult.State newState = asyncResult.AsyncState as CorrelationStateAsyncResult.State;
//                IAsyncResult newAsyncResult = new CorrelationStateAsyncResult(asyncResult, newState.OriginalAsyncState, newState.ExtensibilityCorrelationState);
//                newState.OriginalAsyncCallback(newAsyncResult);
//            }

//            public IAsyncResult BeginRequest(Message message, AsyncCallback callback, object state)
//            {
//                return this.BeginRequest(message, this.DefaultSendTimeout, callback, state);
//            }

//            public Message EndRequest(IAsyncResult result)
//            {
//                CorrelationStateAsyncResult correlationAsyncResult = (CorrelationStateAsyncResult)result;
//                object[] correlationStates = (object[])correlationAsyncResult.CorrelationState;
//                Message reply = this.innerChannel.EndRequest(correlationAsyncResult.OriginalAsyncResult);
//                for (int i = 0; i < this.messageInspectors.Count; i++)
//                {
//                    this.messageInspectors[i].AfterReceiveReply(ref reply, correlationStates[i]);
//                }

//                return reply;
//            }

//            #region Synchronous versions of the methods, not available in SL / WP7
//            public Message Request(Message message, TimeSpan timeout)
//            {
//                throw new NotImplementedException();
//            }

//            public Message Request(Message message)
//            {
//                throw new NotImplementedException();
//            }
//            #endregion

//            #region Methods which simply delegate to the inner channel
//            protected override void OnAbort()
//            {
//                this.innerChannel.Abort();
//            }

//            protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
//            {
//                return this.innerChannel.BeginClose(timeout, callback, state);
//            }

//            protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
//            {
//                return this.innerChannel.BeginOpen(timeout, callback, state);
//            }

//            protected override void OnClose(TimeSpan timeout)
//            {
//                this.innerChannel.Close(timeout);
//            }

//            protected override void OnEndClose(IAsyncResult result)
//            {
//                this.innerChannel.EndClose(result);
//            }

//            protected override void OnEndOpen(IAsyncResult result)
//            {
//                this.innerChannel.EndOpen(result);
//            }

//            protected override void OnOpen(TimeSpan timeout)
//            {
//                this.innerChannel.Open(timeout);
//            }

//            public EndpointAddress RemoteAddress
//            {
//                get { return this.innerChannel.RemoteAddress; }
//            }

//            public Uri Via
//            {
//                get { return this.innerChannel.Via; }
//            }
//            #endregion
//        }
//    }

//    internal class CorrelationStateAsyncResult : IAsyncResult
//    {
//        IAsyncResult originalResult;
//        object correlationState;
//        object originalAsyncState;
//        public CorrelationStateAsyncResult(IAsyncResult originalResult, object originalAsyncState, object correlationState)
//        {
//            this.originalResult = originalResult;
//            this.originalAsyncState = originalAsyncState;
//            this.correlationState = correlationState;
//        }

//        public object AsyncState
//        {
//            get { return this.originalAsyncState; }
//        }

//        public WaitHandle AsyncWaitHandle
//        {
//            get { return this.originalResult.AsyncWaitHandle; }
//        }

//        public bool CompletedSynchronously
//        {
//            get { return this.originalResult.CompletedSynchronously; }
//        }

//        public bool IsCompleted
//        {
//            get { return this.originalResult.IsCompleted; }
//        }

//        internal object CorrelationState
//        {
//            get { return this.correlationState; }
//        }

//        internal IAsyncResult OriginalAsyncResult
//        {
//            get { return this.originalResult; }
//        }

//        internal class State
//        {
//            public AsyncCallback OriginalAsyncCallback { get; set; }
//            public object OriginalAsyncState { get; set; }
//            public object ExtensibilityCorrelationState { get; set; }
//        }
//    }
//}