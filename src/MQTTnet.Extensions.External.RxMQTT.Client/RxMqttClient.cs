using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
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
        private readonly MqttNetSourceLogger logger;
        private readonly Dictionary<string, IObservable<MqttApplicationMessageReceivedEventArgs>> topicSubscriptionCache;
        private readonly object topicSubscriptionLock = new object();

        /// <summary>
        /// Create a rx mqtt client based on a <see cref="ManagedMqttClient"/>.
        /// </summary>
        /// <param name="managedMqttClient">The manged mqtt client.</param>
        /// <param name="logger">The mqtt net logger.</param>
        /// <remarks>
        /// Use the <see cref="MqttFactoryExtensions.CreateRxMqttClient(MqttFactory)"/> or
        /// <see cref="MqttFactoryExtensions.CreateRxMqttClient(MqttFactory, IMqttNetLogger)"/>
        /// factory methods to crate the client.
        /// </remarks>
        /// <exception cref="ArgumentNullException"></exception>
        public RxMqttClient(IManagedMqttClient managedMqttClient, IMqttNetLogger logger)
        {
            InternalClient = managedMqttClient ?? throw new ArgumentNullException(nameof(managedMqttClient));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            this.logger = logger.WithSource(nameof(RxMqttClient));
            topicSubscriptionCache = new Dictionary<string, IObservable<MqttApplicationMessageReceivedEventArgs>>();

            var cancelationSubject = new Subject<Unit>();

            ConnectedEvent = FromAsyncEvent<EventArgs>(
                h => managedMqttClient.ConnectedAsync += h,
                h => managedMqttClient.ConnectedAsync -= h);

            DisconnectedEvent = FromAsyncEvent<EventArgs>(
                h => managedMqttClient.DisconnectedAsync += h,
                h => managedMqttClient.DisconnectedAsync -= h);

            ConnectingFailedEvent = FromAsyncEvent<ConnectingFailedEventArgs>(
                h => managedMqttClient.ConnectingFailedAsync += h,
                h => managedMqttClient.ConnectingFailedAsync -= h);

            SynchronizingSubscriptionsFailedEvent = FromAsyncEvent<ManagedProcessFailedEventArgs>(
                h => managedMqttClient.SynchronizingSubscriptionsFailedAsync += h,
                h => managedMqttClient.SynchronizingSubscriptionsFailedAsync -= h);

            ApplicationMessageProcessedEvent = FromAsyncEvent<ApplicationMessageProcessedEventArgs>(
                h => managedMqttClient.ApplicationMessageProcessedAsync += h,
                h => managedMqttClient.ApplicationMessageProcessedAsync -= h);

            ApplicationMessageSkippedEvent = FromAsyncEvent<ApplicationMessageSkippedEventArgs>(
                h => managedMqttClient.ApplicationMessageSkippedAsync += h,
                h => managedMqttClient.ApplicationMessageSkippedAsync -= h);

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

            applicationMessageReceived = FromAsyncEvent<MqttApplicationMessageReceivedEventArgs>(
                h => managedMqttClient.ApplicationMessageReceivedAsync += h,
                h => managedMqttClient.ApplicationMessageReceivedAsync -= h);

            IObservable<T> FromAsyncEvent<T>(Action<Func<T, Task>> addHandler, Action<Func<T, Task>> removeHandler)
            {
                return Observable
                    .Create<T>(observer =>
                    {
                        Task Delegate(T args)
                        {
                            observer.OnNext(args);
                            return Task.CompletedTask;
                        }
                        addHandler(Delegate);
                        return Disposable.Create(() => removeHandler(Delegate));
                    })
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
        public IObservable<EventArgs> ConnectedEvent { get; }

        /// <inheritdoc/>
        public IObservable<ConnectingFailedEventArgs> ConnectingFailedEvent { get; }

        /// <inheritdoc/>
        public IObservable<EventArgs> DisconnectedEvent { get; }

        /// <inheritdoc/>
        public IManagedMqttClient InternalClient { get; }

        /// <inheritdoc/>
        public bool IsConnected => InternalClient.IsConnected;

        /// <inheritdoc/>
        public bool IsStarted => InternalClient.IsStarted;

        /// <inheritdoc/>
        public ManagedMqttClientOptions Options => InternalClient.Options;

        /// <inheritdoc/>
        public int PendingApplicationMessagesCount => InternalClient.PendingApplicationMessagesCount;

        /// <inheritdoc/>
        public IObservable<ManagedProcessFailedEventArgs> SynchronizingSubscriptionsFailedEvent { get; }

        /// <inheritdoc/>
        /// <exception cref="ArgumentException"></exception>
        public IObservable<MqttApplicationMessageReceivedEventArgs> Connect(string topic)
        {
            if (string.IsNullOrWhiteSpace(topic))
                throw new ArgumentException($"'{nameof(topic)}' cannot be null or whitespace", nameof(topic));

            ThrowIfDisposed();
            lock (topicSubscriptionLock)
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
                                var mqttTopicFilter = new MqttTopicFilterBuilder()
                                    .WithTopic(topic)
                                    .Build();
                                await InternalClient.SubscribeAsync(new[] { mqttTopicFilter }).ConfigureAwait(false);
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
                                    lock (topicSubscriptionLock)
                                    {
                                        messageSubscription.Dispose();
                                        topicSubscriptionCache.Remove(topic);
                                    }
                                    try
                                    {
                                        await InternalClient.UnsubscribeAsync(new[] { topic }).ConfigureAwait(false);
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
        public Task PublishAsync(ManagedMqttApplicationMessage applicationMessage)
        {
            if (applicationMessage is null) throw new ArgumentNullException(nameof(applicationMessage));

            return InternalClient.EnqueueAsync(applicationMessage);
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException"></exception>
        public Task PublishAsync(MqttApplicationMessage applicationMessage)
        {
            if (applicationMessage is null) throw new ArgumentNullException(nameof(applicationMessage));

            return InternalClient.EnqueueAsync(applicationMessage);
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentNullException"></exception>
        public Task StartAsync(ManagedMqttClientOptions options)
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