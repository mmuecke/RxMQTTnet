using Autofac.Extras.Moq;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
using Moq;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Extensions.External.RxMQTT.Client;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Xunit;
using MQTTnet.Diagnostics;

namespace MQTTnet.Extensions.External.RxMQTT.Client.Test
{
    public class MangedClientWrapperTest
    {
        [Fact]
        public void Connected_Returns_False_Init()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>();
            var rxMqttClinet = mock.Create<RxMqttClinet>();

            var testScheduler = new TestScheduler();

            // act
            var testObserver = testScheduler.Start(() => rxMqttClinet.Connected, 0, 0, 1);

            Assert.Single(testObserver.Messages);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Single().Value.Kind);
            Assert.False(testObserver.Messages.Single().Value.Value);
        }

        [Fact]
        public void Connected_Observer()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>().SetupProperty(x => x.ConnectedHandler);
            var rxMqttClinet = mock.Create<RxMqttClinet>();

            var testScheduler = new TestScheduler();

            testScheduler.ScheduleAsync(TimeSpan.FromTicks(3), (_, __) => mock.Mock<IManagedMqttClient>().Object.ConnectedHandler.HandleConnectedAsync(new MqttClientConnectedEventArgs(new MqttClientAuthenticateResult())));
            // act
            var testObserver = testScheduler.Start(() => rxMqttClinet.Connected, 0, 0, 4);

            Assert.Equal(2, testObserver.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Last().Value.Kind);
            Assert.True(testObserver.Messages.Last().Value.Value);
        }

        [Fact]
        public void Disconnected_Observer()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>().SetupProperty(x => x.ConnectedHandler)
            .SetupProperty(x => x.DisconnectedHandler);
            var rxMqttClinet = mock.Create<RxMqttClinet>();

            var testScheduler = new TestScheduler();

            testScheduler.ScheduleAsync(TimeSpan.FromTicks(2), (_, __) => mock.Mock<IManagedMqttClient>().Object.ConnectedHandler.HandleConnectedAsync(new MqttClientConnectedEventArgs(new MqttClientAuthenticateResult())));
            testScheduler.ScheduleAsync(TimeSpan.FromTicks(3), (_, __) => mock.Mock<IManagedMqttClient>().Object.DisconnectedHandler.HandleDisconnectedAsync(new MqttClientDisconnectedEventArgs(true, new Exception(), new MqttClientAuthenticateResult(), MqttClientDisconnectReason.KeepaliveTimeout)));
            // act
            var testObserver = testScheduler.Start(() => rxMqttClinet.Connected, 0, 0, 4);

            Assert.Equal(3, testObserver.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Last().Value.Kind);
            Assert.False(testObserver.Messages.Last().Value.Value);
        }

        [Fact]
        public void ConnectingFailedHandler()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ConnectingFailedHandler);
            var rxMqttClinet = mock.Create<RxMqttClinet>();

            var testScheduler = new TestScheduler();

            var @event = new ManagedProcessFailedEventArgs(new Exception());
            testScheduler.ScheduleAsync(TimeSpan.FromTicks(2), (_, __) => mock.Mock<IManagedMqttClient>().Object.ConnectingFailedHandler.HandleConnectingFailedAsync(@event));

            // act
            var testObserver = testScheduler.Start(() => rxMqttClinet.ConnectingFailedEvent, 0, 0, 4);

            Assert.Equal(1, testObserver.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Last().Value.Kind);
            Assert.Equal(@event, testObserver.Messages.Last().Value.Value);
        }

        [Fact]
        public void SynchronizingSubscriptionsFailedHandler()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.SynchronizingSubscriptionsFailedHandler);
            var rxMqttClinet = mock.Create<RxMqttClinet>();

            var testScheduler = new TestScheduler();

            var @event = new ManagedProcessFailedEventArgs(new Exception());
            testScheduler.ScheduleAsync(TimeSpan.FromTicks(2), (_, __) => mock.Mock<IManagedMqttClient>().Object.SynchronizingSubscriptionsFailedHandler.HandleSynchronizingSubscriptionsFailedAsync(@event));

            // act
            var testObserver = testScheduler.Start(() => rxMqttClinet.SynchronizingSubscriptionsFailedEvent, 0, 0, 4);

            Assert.Equal(1, testObserver.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Last().Value.Kind);
            Assert.Equal(@event, testObserver.Messages.Last().Value.Value);
        }

        [Fact]
        public void Factory()
        {
            // act
            var client = new MqttFactory().CreateRxMqttClient();

            // test
            Assert.NotNull(client);
            Assert.IsType<RxMqttClinet>(client);
        }

        [Fact]
        public void Factory_NullException()
        {
            // act
            Assert.Throws<ArgumentNullException>(() => ((MqttFactory)null).CreateRxMqttClient());
        }

        [Fact]
        public void Factory_With_Logger()
        {
            var looger = new MqttNetLogger("MyCustomId");
            // act
            var client = new MqttFactory().CreateRxMqttClient(looger);

            // test
            Assert.NotNull(client);
            Assert.IsType<RxMqttClinet>(client);
        }

        [Fact]
        public void Factory_NullException_With_Logger()
        {
            var looger = new MqttNetLogger("MyCustomId");
            // act
            Assert.Throws<ArgumentNullException>(() => ((MqttFactory)null).CreateRxMqttClient(looger));
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
            var rxMqttClinet = mock.Create<RxMqttClinet>();

            // act
            await rxMqttClinet.StartAsync(options);

            // test
            mock.Mock<IManagedMqttClient>().Verify(x => x.StartAsync(options));
            Assert.Equal(options, rxMqttClinet.Options);
        }

        [Fact]
        public async void PingAsync()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>();
            var rxMqttClinet = mock.Create<RxMqttClinet>();

            var cts = new CancellationToken();

            // act
            await rxMqttClinet.PingAsync(cts);

            // test
            mock.Mock<IManagedMqttClient>().Verify(x => x.PingAsync(cts));
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
            var rxMqttClinet = mock.Create<RxMqttClinet>();

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
            var rxMqttClinet = mock.Create<RxMqttClinet>();

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
            var rxMqttClinet = mock.Create<RxMqttClinet>();

            // act
            await rxMqttClinet.StartAsync(options);

            // test
            mock.Mock<IManagedMqttClient>().Verify(x => x.StartAsync(options));
        }

        [Fact]
        public async void StopAsync()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>();
            var rxMqttClinet = mock.Create<RxMqttClinet>();

            // act
            await rxMqttClinet.StopAsync();

            // test
            mock.Mock<IManagedMqttClient>().Verify(x => x.StopAsync());
        }
    }
}