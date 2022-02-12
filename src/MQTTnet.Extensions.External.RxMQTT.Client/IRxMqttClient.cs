using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.External.RxMQTT.Client
{
    /// <summary>
    /// A rx mqtt client based on a <see cref="ManagedMqttClient"/>.
    /// </summary>
    /// <remarks>
    /// Use the <see cref="MqttFactoryExtensions.CreateRxMqttClient(MqttFactory)"/> or
    /// <see cref="MqttFactoryExtensions.CreateRxMqttClient(MqttFactory, IMqttNetLogger)"/>
    /// factory methods to crate the client.
    /// </remarks>
    public interface IRxMqttClient : IDisposable
    {
        /// <summary>
        /// Observer for the connection state of the client.
        /// </summary>
        IObservable<bool> Connected { get; }

        /// <summary>
        /// Observer for the connected event.
        /// </summary>
        IObservable<EventArgs> ConnectedEvent { get; }

        /// <summary>
        /// Observer for the connection failed event.
        /// </summary>
        IObservable<ConnectingFailedEventArgs> ConnectingFailedEvent { get; }

        /// <summary>
        /// Observer for the disconnected event.
        /// </summary>
        IObservable<EventArgs> DisconnectedEvent { get; }

        /// <summary>
        /// Gets the internally used MQTT client.
        /// </summary>
        /// <remarks>
        /// This property should be used with caution because manipulating the internal client might break the rx client.
        /// </remarks>
        ManagedMqttClient InternalClient { get; }

        /// <summary>
        /// The connection state of the client.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// The started state of the client.
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// The options set for the client.
        /// </summary>
        ManagedMqttClientOptions Options { get; }

        /// <summary>
        /// The amount of pending messages.
        /// </summary>
        int PendingApplicationMessagesCount { get; }

        /// <summary>
        /// Observer for events when subscribing failed.
        /// </summary>
        IObservable<ManagedProcessFailedEventArgs> SynchronizingSubscriptionsFailedEvent { get; }

        /// <summary>
        /// Observer for events when message was processed.
        /// </summary>
        IObservable<ApplicationMessageProcessedEventArgs> ApplicationMessageProcessedEvent { get; }

        /// <summary>
        /// Observer for events when message was skipped.
        /// </summary>
        IObservable<ApplicationMessageSkippedEventArgs> ApplicationMessageSkippedEvent { get; }

        /// <summary>
        /// Connect to a subscription to the <paramref name="topic"/>.
        /// </summary>
        /// <param name="topic">The topic to subscribe.</param>
        /// <returns>A observer for the messages on the <paramref name="topic"/>.</returns>
        IObservable<MqttApplicationMessageReceivedEventArgs> Connect(string topic);

        /// <summary>
        /// Ping the server.
        /// </summary>
        /// <param name="cancellationToken">Token to interrupt the request.</param>
        /// <returns>The ping task.</returns>
        Task PingAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Publish a message.
        /// </summary>
        /// <param name="applicationMessage">The message to publish.</param>
        /// <returns>The publish task.</returns>
        Task PublishAsync(ManagedMqttApplicationMessage applicationMessage);

        /// <summary>
        /// Publish a message.
        /// </summary>
        /// <param name="applicationMessage">The message to publish.</param>
        /// <returns>The publish task.</returns>
        Task PublishAsync(MqttApplicationMessage applicationMessage);

        /// <summary>
        /// Start the client whit the <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The options for the client.</param>
        /// <returns>The start task.</returns>
        Task StartAsync(ManagedMqttClientOptions options);

        /// <summary>
        /// Stops the client.
        /// </summary>
        /// <returns>The stop task.</returns>
        Task StopAsync();
    }
}