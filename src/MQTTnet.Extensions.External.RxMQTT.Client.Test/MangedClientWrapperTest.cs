using Autofac.Extras.Moq;
using Microsoft.Reactive.Testing;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using Xunit;

namespace MQTTnet.Extensions.External.RxMQTT.Client.Test
{
    public class MangedClientWrapperTest
    {
        [Fact]
        public void Connected_Observer()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>().SetupProperty(x => x.ConnectedHandler);
            var rxMqttClinet = mock.Create<RxMqttClient>();

            var testScheduler = new TestScheduler();

            testScheduler.ScheduleAsync(TimeSpan.FromTicks(3), (_, __) => mock.Mock<IManagedMqttClient>().Object.ConnectedHandler.HandleConnectedAsync(new MqttClientConnectedEventArgs(new MqttClientAuthenticateResult())));
            // act
            var testObserver = testScheduler.Start(() => rxMqttClinet.Connected, 0, 0, 4);

            Assert.Equal(2, testObserver.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Last().Value.Kind);
            Assert.True(testObserver.Messages.Last().Value.Value);
            testScheduler.AdvanceBy(1);
            Assert.Null(rxMqttClinet.InternalClient.ConnectedHandler);
            Assert.Null(rxMqttClinet.InternalClient.DisconnectedHandler);
        }

        [Fact]
        public void Connected_Returns_False_Init()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>();
            var rxMqttClinet = mock.Create<RxMqttClient>();

            var testScheduler = new TestScheduler();

            // act
            var testObserver = testScheduler.Start(() => rxMqttClinet.Connected, 0, 0, 1);

            Assert.Single(testObserver.Messages);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Single().Value.Kind);
            Assert.False(testObserver.Messages.Single().Value.Value);
        }

        [Fact]
        public void ConnectingFailedHandler()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ConnectingFailedHandler);
            var rxMqttClinet = mock.Create<RxMqttClient>();

            var testScheduler = new TestScheduler();

            var @event = new ManagedProcessFailedEventArgs(new Exception());
            testScheduler.ScheduleAsync(TimeSpan.FromTicks(2), (_, __) => mock.Mock<IManagedMqttClient>().Object.ConnectingFailedHandler.HandleConnectingFailedAsync(@event));

            // act
            var testObserver = testScheduler.Start(() => rxMqttClinet.ConnectingFailedEvent, 0, 0, 4);

            Assert.Equal(1, testObserver.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Last().Value.Kind);
            Assert.Equal(@event, testObserver.Messages.Last().Value.Value);

            Assert.Null(rxMqttClinet.InternalClient.ConnectingFailedHandler);
        }

        [Fact]
        public void CTOR_ArgumentNullException_Logger()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            var managedMqttClient = mock.Create<IManagedMqttClient>(); ;
            IMqttNetLogger logger = null;
            Assert.Throws<ArgumentNullException>(() => new RxMqttClinet(managedMqttClient, logger));
        }

        [Fact]
        public void CTOR_ArgumentNullException_ManagedMqttClient()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IMqttNetLogger>();
            IManagedMqttClient managedMqttClient = null;
            var logger = mock.Create<IMqttNetLogger>();

            Assert.Throws<ArgumentNullException>(() => new RxMqttClinet(managedMqttClient, logger));
        }

        [Fact]
        public void Disconnected_Observer()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>().SetupProperty(x => x.ConnectedHandler)
            .SetupProperty(x => x.DisconnectedHandler);
            var rxMqttClinet = mock.Create<RxMqttClient>();

            var testScheduler = new TestScheduler();

            testScheduler.ScheduleAsync(TimeSpan.FromTicks(2), (_, __) => mock.Mock<IManagedMqttClient>().Object.ConnectedHandler.HandleConnectedAsync(new MqttClientConnectedEventArgs(new MqttClientAuthenticateResult())));
            testScheduler.ScheduleAsync(TimeSpan.FromTicks(3), (_, __) => mock.Mock<IManagedMqttClient>().Object.DisconnectedHandler.HandleDisconnectedAsync(new MqttClientDisconnectedEventArgs(true, new Exception(), new MqttClientAuthenticateResult(), MqttClientDisconnectReason.KeepaliveTimeout)));
            // act
            var testObserver = testScheduler.Start(() => rxMqttClinet.Connected, 0, 0, 4);

            Assert.Equal(3, testObserver.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Last().Value.Kind);
            Assert.False(testObserver.Messages.Last().Value.Value);
            Assert.Null(rxMqttClinet.InternalClient.ConnectedHandler);
            Assert.Null(rxMqttClinet.InternalClient.DisconnectedHandler);
        }

        [Fact]
        public void Factory()
        {
            // act
            var client = new MqttFactory().CreateRxMqttClient();

            // test
            Assert.NotNull(client);
            Assert.IsType<RxMqttClient>(client);
        }

        [Fact]
        public void Factory_NullException()
        {
            // act
            Assert.Throws<ArgumentNullException>(() => ((MqttFactory)null).CreateRxMqttClient());
        }

        [Fact]
        public void Factory_NullException_With_Logger()
        {
            var looger = new MqttNetLogger("MyCustomId");
            // act
            Assert.Throws<ArgumentNullException>(() => ((MqttFactory)null).CreateRxMqttClient(looger));
        }

        [Fact]
        public void Factory_With_Logger()
        {
            var looger = new MqttNetLogger("MyCustomId");
            // act
            var client = new MqttFactory().CreateRxMqttClient(looger);

            // test
            Assert.NotNull(client);
            Assert.IsType<RxMqttClient>(client);
        }

        [Fact]
        public void Factory_With_Logger_NullException()
        {
            // act
            Assert.Throws<ArgumentNullException>(() => new MqttFactory().CreateRxMqttClient(null));
        }

        [Fact]
        public async void Options()
        {
            using var mock = AutoMock.GetLoose();

            var options = new ManagedMqttClientOptionsBuilder()
           .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
           .WithClientOptions(new MqttClientOptionsBuilder()
               .WithClientId("Client1")
               .WithTcpServer("broker.hivemq.com")
               .WithTls().Build())
           .Build();

            mock.Mock<IManagedMqttClient>()
                .Setup(x => x.Options)
                .Returns(options);
            var rxMqttClinet = mock.Create<RxMqttClient>();

            // act
            await rxMqttClinet.StartAsync(options);

            // test
            mock.Mock<IManagedMqttClient>().Verify(x => x.StartAsync(options));
            Assert.Equal(options, rxMqttClinet.Options);
        }

        [Fact]
        public void PendingApplicationMessagesCount()
        {
            var count = 10;
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>()
                .Setup(x => x.PendingApplicationMessagesCount)
                .Returns(count);

            // act
            var rxMqttClinet = mock.Create<RxMqttClient>();

            // test
            Assert.Equal(count, rxMqttClinet.PendingApplicationMessagesCount);
        }

        [Fact]
        public async void PingAsync()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>();
            var rxMqttClinet = mock.Create<RxMqttClient>();

            var cts = new CancellationToken();

            // act
            await rxMqttClinet.PingAsync(cts);

            // test
            mock.Mock<IManagedMqttClient>().Verify(x => x.PingAsync(cts));
        }

        [Fact]
        public async void PublishAsync()
        {
            using var mock = AutoMock.GetLoose();
            var message = new ManagedMqttApplicationMessage();

            mock.Mock<IManagedMqttClient>();
            var rxMqttClinet = mock.Create<RxMqttClient>();

            // act
            await rxMqttClinet.PublishAsync(message);

            // test
            mock.Mock<IManagedMqttClient>().Verify(x => x.PublishAsync(message));
        }

        [Fact]
        public void PublishAsync_ArgumentNullException()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            RxMqttClient rxMqttClinet = mock.Create<RxMqttClient>();

            // act
            _ = Assert.ThrowsAsync<ArgumentNullException>(() => rxMqttClinet.PublishAsync(null));
        }

        [Fact]
        public async void PublishAsync_CancellationToken()
        {
            using var mock = AutoMock.GetLoose();
            var ct = new CancellationToken();
            var message = new MqttApplicationMessage();

            mock.Mock<IManagedMqttClient>();
            var rxMqttClinet = mock.Create<RxMqttClient>();

            // act
            await rxMqttClinet.PublishAsync(message, ct);

            // test
            mock.Mock<IManagedMqttClient>().Verify(x => x.PublishAsync(message, ct));
        }

        [Fact]
        public void PublishAsync_CancellationToken_ArgumentNullException()
        {
            using var mock = AutoMock.GetLoose();
            var ct = new CancellationToken();
            mock.Mock<IManagedMqttClient>();
            var rxMqttClinet = mock.Create<RxMqttClient>();

            // act
            _ = Assert.ThrowsAsync<ArgumentNullException>(() => rxMqttClinet.PublishAsync(null, ct));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SetIsConnected(bool isStarted)
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>()
                .Setup(x => x.IsConnected)
                .Returns(isStarted);

            // act
            var rxMqttClinet = mock.Create<RxMqttClient>();

            // test
            Assert.Equal(isStarted, rxMqttClinet.IsConnected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SetIsStarted(bool isStarted)
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>()
                .Setup(x => x.IsStarted)
                .Returns(isStarted);

            // act
            var rxMqttClinet = mock.Create<RxMqttClient>();

            // test
            Assert.Equal(isStarted, rxMqttClinet.IsStarted);
        }

        [Fact]
        public async void StartAsync()
        {
            using var mock = AutoMock.GetLoose();
            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId("Client1")
                    .WithTcpServer("broker.hivemq.com")
                    .WithTls().Build())
                .Build();

            mock.Mock<IManagedMqttClient>();
            var rxMqttClinet = mock.Create<RxMqttClient>();

            // act
            await rxMqttClinet.StartAsync(options);

            // test
            mock.Mock<IManagedMqttClient>().Verify(x => x.StartAsync(options));
        }

        [Fact]
        public void StartAsync_ArgumentNullException()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            var rxMqttClinet = mock.Create<RxMqttClient>();

            // act
            _ = Assert.ThrowsAsync<ArgumentNullException>(() => rxMqttClinet.StartAsync(null));
        }

        [Fact]
        public async void StopAsync()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>();
            var rxMqttClinet = mock.Create<RxMqttClient>();

            // act
            await rxMqttClinet.StopAsync();

            // test
            mock.Mock<IManagedMqttClient>().Verify(x => x.StopAsync());
        }

        [Fact]
        public void SynchronizingSubscriptionsFailedHandler()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.SynchronizingSubscriptionsFailedHandler);
            var rxMqttClinet = mock.Create<RxMqttClient>();

            var testScheduler = new TestScheduler();

            var @event = new ManagedProcessFailedEventArgs(new Exception());
            testScheduler.ScheduleAsync(TimeSpan.FromTicks(2), (_, __) => mock.Mock<IManagedMqttClient>().Object.SynchronizingSubscriptionsFailedHandler.HandleSynchronizingSubscriptionsFailedAsync(@event));

            // act
            var testObserver = testScheduler.Start(() => rxMqttClinet.SynchronizingSubscriptionsFailedEvent, 0, 0, 4);

            Assert.Equal(1, testObserver.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Last().Value.Kind);
            Assert.Equal(@event, testObserver.Messages.Last().Value.Value);
            Assert.Null(rxMqttClinet.InternalClient.SynchronizingSubscriptionsFailedHandler);
        }

        [Fact]
        public void ApplicationMessageProcessedHandler()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageProcessedHandler);
            var rxMqttClinet = mock.Create<RxMqttClient>();

            var testScheduler = new TestScheduler();

            var message = new ManagedMqttApplicationMessage();
            var @event = new ApplicationMessageProcessedEventArgs(message, new Exception());
            testScheduler.ScheduleAsync(TimeSpan.FromTicks(2), (_, __) => mock.Mock<IManagedMqttClient>().Object.ApplicationMessageProcessedHandler.HandleApplicationMessageProcessedAsync(@event));

            // act
            var testObserver = testScheduler.Start(() => rxMqttClinet.ApplicationMessageProcessedEvent, 0, 0, 4);

            Assert.Equal(1, testObserver.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Last().Value.Kind);
            Assert.Equal(@event, testObserver.Messages.Last().Value.Value);
            Assert.Null(rxMqttClinet.InternalClient.SynchronizingSubscriptionsFailedHandler);
        }

        [Fact]
        public void ApplicationMessageSkippedHandler()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageSkippedHandler);
            var rxMqttClinet = mock.Create<RxMqttClient>();

            var testScheduler = new TestScheduler();

            var message = new ManagedMqttApplicationMessage();
            var @event = new ApplicationMessageSkippedEventArgs(message);
            testScheduler.ScheduleAsync(TimeSpan.FromTicks(2), (_, __) => mock.Mock<IManagedMqttClient>().Object.ApplicationMessageSkippedHandler.HandleApplicationMessageSkippedAsync(@event));

            // act
            var testObserver = testScheduler.Start(() => rxMqttClinet.ApplicationMessageSkippedEvent, 0, 0, 4);

            Assert.Equal(1, testObserver.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Last().Value.Kind);
            Assert.Equal(@event, testObserver.Messages.Last().Value.Value);
            Assert.Null(rxMqttClinet.InternalClient.SynchronizingSubscriptionsFailedHandler);
        }
    }
}