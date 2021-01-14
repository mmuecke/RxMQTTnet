using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Publishing;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.External.RxMQTT.Client
{
    /// <summary>
    /// A mqtt client using <see cref="System.Reactive"/> for subscribing to topics.
    /// </summary>
    public class RxMqttClient : Internal.Disposable, IRxMqttClient
    {
        private readonly IObservable<MqttApplicationMessageReceivedEventArgs> applicationMessageReceived;

        private readonly IDisposable cleanUp;
        private readonly IMqttNetScopedLogger logger;
        private readonly Dictionary<string, IObservable<MqttApplicationMessageReceivedEventArgs>> topicSubscriptionCache;

        /// <summary>
        /// Create a rx mqtt client based on a <see cref="IManagedMqttClient"/>.
        /// </summary>
        /// <param name="managedMqttClient">The manged mqtt client.</param>
        /// <param name="logger">The mqtt net logger.</param>
        /// <remarks>
        /// Use the <see cref="MqttFactoryExtensions.CreateRxMqttClient(IMqttFactory)"/> or
        /// <see cref="MqttFactoryExtensions.CreateRxMqttClient(IMqttFactory, IMqttNetLogger)"/>
        /// factory methods to crate the client.
        /// </remarks>
        /// <exception cref="ArgumentNullException"></exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA2000:Dispose objects before losing scope", Justification = "Is disposed wiht the class.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception on dispose.")]
        public RxMqttClient(IManagedMqttClient managedMqttClient, IMqttNetLogger logger)
        {
            InternalClient = managedMqttClient ?? throw new ArgumentNullException(nameof(managedMqttClient));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            this.logger = logger.CreateScopedLogger(nameof(RxMqttClient));
            topicSubscriptionCache = new Dictionary<string, IObservable<MqttApplicationMessageReceivedEventArgs>>();

            var cancelationSubject = new Subject<Unit>();

            ConnectedEvent = CrateFromHandler<MqttClientConnectedEventArgs>(observer =>
                {
                    managedMqttClient.UseConnectedHandler(args => observer.OnNext(args));
                    return Disposable.Create(() => managedMqttClient.ConnectedHandler = null);
                });

            DisconnectedEvent = CrateFromHandler<MqttClientDisconnectedEventArgs>(observer =>
                {
                    managedMqttClient.UseDisconnectedHandler(args => observer.OnNext(args));
                    return Disposable.Create(() => managedMqttClient.DisconnectedHandler = null);
                });

            ConnectingFailedEvent = CrateFromHandler<ManagedProcessFailedEventArgs>(observer =>
                {
                    managedMqttClient.ConnectingFailedHandler = new ConnectingFailedHandlerDelegate(args => observer.OnNext(args));
                    return Disposable.Create(() => managedMqttClient.ConnectingFailedHandler = null);
                });

            SynchronizingSubscriptionsFailedEvent = CrateFromHandler<ManagedProcessFailedEventArgs>(observer =>
                {
                    managedMqttClient.SynchronizingSubscriptionsFailedHandler = new SynchronizingSubscriptionsFailedHandlerDelegate(args => observer.OnNext(args));
                    return Disposable.Create(() => managedMqttClient.SynchronizingSubscriptionsFailedHandler = null);
                });

            ApplicationMessageProcessedEvent = CrateFromHandler<ApplicationMessageProcessedEventArgs>(observer =>
                {
                    managedMqttClient.ApplicationMessageProcessedHandler = new ApplicationMessageProcessedHandlerDelegate(args => observer.OnNext(args));
                    return Disposable.Create(() => managedMqttClient.ApplicationMessageReceivedHandler = null);
                });

            ApplicationMessageSkippedEvent = CrateFromHandler<ApplicationMessageSkippedEventArgs>(observer =>
                {
                    managedMqttClient.ApplicationMessageSkippedHandler = new ApplicationMessageSkippedHandlerDelegate(args => observer.OnNext(args));
                    return Disposable.Create(() => managedMqttClient.ApplicationMessageReceivedHandler = null);
                });

            Connected = Observable
                .Create<bool>(observer =>
                {
                    var connected = ConnectedEvent.Subscribe(_ => observer.OnNext(true));
                    var disconnected = DisconnectedEvent.Subscribe(_ => observer.OnNext(false));
                    return new CompositeDisposable(connected, disconnected);
                })
                .TakeUntil(cancelationSubject)      // complete on dispose
                .Prepend(IsConnected)               // start with current state
                .Append(false)                      // finish with false
                .Replay(1)                          // replay last state on subscribe
                .RefCount();                        // count subscriptions and dispose source observable when no subscription

            applicationMessageReceived = CrateFromHandler<MqttApplicationMessageReceivedEventArgs>(observer =>
                {
                    managedMqttClient.UseApplicationMessageReceivedHandler(args => observer.OnNext(args));
                    return Disposable.Create(() => managedMqttClient.ApplicationMessageReceivedHandler = null);
                });

            IObservable<T> CrateFromHandler<T>(Func<IObserver<T>, IDisposable> func)
            {
                return Observable.Create(func)
                    .TakeUntil(cancelationSubject)  // complete on dispose
                    .Publish()                      // publish from on source observable
                    .RefCount();                    // count subscriptions and dispose source observable when no subscription
            }

            cleanUp = Disposable.Create(() =>
            {
                cancelationSubject.OnNext(Unit.Default);    // complete all observers
                cancelationSubject.Dispose();
                try { managedMqttClient.Dispose(); }
                catch { }
            });
        }

        /// <inheritdoc/>
        public IObservable<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessedEvent { get; }

        /// <inheritdoc/>
        public IObservable<ApplicationMessageSkippedEventArgs> ApplicationMessageSkippedEvent { get; }

        /// <inheritdoc/>
        public IObservable<bool> Connected { get; }

        /// <inheritdoc/>
        public IObservable<MqttClientConnectedEventArgs> ConnectedEvent { get; }

        /// <inheritdoc/>
        public IObservable<ManagedProcessFailedEventArgs> ConnectingFailedEvent { get; }

        /// <inheritdoc/>
        public IObservable<MqttClientDisconnectedEventArgs> DisconnectedEvent { get; }

        /// <inheritdoc/>
        public IManagedMqttClient InternalClient { get; }

        /// <inheritdoc/>
        public bool IsConnected => InternalClient.IsConnected;

        /// <inheritdoc/>
        public bool IsStarted => InternalClient.IsStarted;

        /// <inheritdoc/>
        public IManagedMqttClientOptions Options => InternalClient.Options;

        /// <inheritdoc/>
        public int PendingApplicationMessagesCount => InternalClient.PendingApplicationMessagesCount;

        /// <inheritdoc/>
        public IObservable<ManagedProcessFailedEventArgs> SynchronizingSubscriptionsFailedEvent { get; }

        /// <inheritdoc/>
        /// <exception cref="ArgumentException"></exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Logged or forwarded.")]
        public IObservable<MqttApplicationMessageReceivedEventArgs> Connect(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                throw new ArgumentException($"'{nameof(topic)}' cannot be null or whitespace", nameof(topic));

            ThrowIfDisposed();
            lock (topicSubscriptionCache)
            {
                // try get exiting observable for topic
                if (!topicSubscriptionCache.TryGetValue(topic, out IObservable<MqttApplicationMessageReceivedEventArgs> observable))
                {
                    // create new observable for topic
                    observable = Observable
                        .Create<MqttApplicationMessageReceivedEventArgs>(async observer =>
                        {
                            // subscribe to topic
                            try
                            {
                                await InternalClient.SubscribeAsync(topic).ConfigureAwait(false);
                            }
                            catch (Exception exception)
                            {
                                logger.Error(exception, "Error while maintaining subscribe from topic.");
                                observer.OnError(exception);
                                return Disposable.Empty;
                            }

                            // filter all received messages
                            // and subscribe to messages for this topic
                            var messageSubscription = applicationMessageReceived
                                .FilterTopic(topic)
                                .Subscribe(observer);

                            return Disposable.Create(async () =>
                                {
                                    // clean up subscription when no observer subscribed
                                    lock (topicSubscriptionCache)
                                    {
                                        messageSubscription.Dispose();
                                        topicSubscriptionCache.Remove(topic);
                                    }
                                    try
                                    {
                                        await InternalClient.UnsubscribeAsync(topic).ConfigureAwait(false);
                                    }
                                    catch (ObjectDisposedException) { } // if disposed there is nothing to unsubscribe
                                    catch (Exception exception)
                                    {
                                        logger.Error(exception, "Error while maintaining unsubscribe from topic.");
                                    }
                                });
                        })
                        .Publish()      // publish from on source observable
                        .RefCount();    // count subscriptions and dispose source observable when no subscription

                    // save observable for topic
                    lock (topicSubscriptionCache)
                        topicSubscriptionCache.Add(topic, observable);
                }
                return observable;
            }
        }

        /// <inheritdoc/>
        public Task PingAsync(CancellationToken cancellationToken)
        {
            return InternalClient.PingAsync(cancellationToken);
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException"></exception>
        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            if (applicationMessage is null) throw new ArgumentNullException(nameof(applicationMessage));

            return InternalClient.PublishAsync(applicationMessage, cancellationToken);
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException"></exception>
        public Task PublishAsync(ManagedMqttApplicationMessage applicationMessage)
        {
            if (applicationMessage is null) throw new ArgumentNullException(nameof(applicationMessage));

            return InternalClient.PublishAsync(applicationMessage);
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException"></exception>
        public Task StartAsync(IManagedMqttClientOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));

            return InternalClient.StartAsync(options);
        }

        /// <inheritdoc/>
        public Task StopAsync()
        {
            return InternalClient.StopAsync();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            cleanUp.Dispose();
            base.Dispose(disposing);
        }
    }
}