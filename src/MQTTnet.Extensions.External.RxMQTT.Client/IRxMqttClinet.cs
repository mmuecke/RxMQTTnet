using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Extensions.External.RxMQTT.Client
{
    /// <summary>
    /// A rx mqtt client based on a <see cref="IManagedMqttClient"/>.
    /// </summary>
    /// <remarks>
    /// Use the <see cref="MqttFactoryExtensions.CreateRxMqttClient(IMqttFactory)"/> or
    /// <see cref="MqttFactoryExtensions.CreateRxMqttClient(IMqttFactory, Diagnostics.IMqttNetLogger)"/>
    /// factory methods to crate the client.
    /// </remarks>
    public interface IRxMqttClinet : IApplicationMessagePublisher, IDisposable
    {
        /// <summary>
        /// Observer for the connection state of the client.
        /// </summary>
        IObservable<bool> Connected { get; }

        /// <summary>
        /// Observer for the connected event.
        /// </summary>
        IObservable<MqttClientConnectedEventArgs> ConnectedEvent { get; }

        /// <summary>
        /// Observer for the connection failed event.
        /// </summary>
        IObservable<ManagedProcessFailedEventArgs> ConnectingFailed { get; }

        /// <summary>
        /// Observer for the disconnected event.
        /// </summary>
        IObservable<MqttClientDisconnectedEventArgs> DisconnectedEvent { get; }

        /// <summary>
        /// Gets the internally used MQTT client.
        /// </summary>
        /// <remarks>
        /// This property should be used with caution because manipulating the internal client might break the rx client.
        /// </remarks>
        IManagedMqttClient InternalClient { get; }

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
        IManagedMqttClientOptions Options { get; }

        /// <summary>
        /// The amount of pending messages.
        /// </summary>
        int PendingApplicationMessagesCount { get; }

        /// <summary>
        /// Observer for events when subscribing failed.
        /// </summary>
        IObservable<ManagedProcessFailedEventArgs> SynchronizingSubscriptionsFailed { get; }

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
        /// Start the client whit the <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The options for the client.</param>
        /// <returns>The start task.</returns>
        Task StartAsync(IManagedMqttClientOptions options);

        /// <summary>
        /// Stops the client.
        /// </summary>
        /// <returns>The stop task.</returns>
        Task StopAsync();
    }
}