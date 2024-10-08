using Autofac.Extras.Moq;
using Microsoft.Reactive.Testing;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MQTTnet.Extensions.External.RxMQTT.Client.Test
{
    public class MangedClientWrapperTest
    {
        [Fact]
        public void ApplicationMessageProcessedHandler()
        {
            using var mock = AutoMock.GetLoose();
            var rxMqttClient = mock.Create<RxMqttClient>();

            var testScheduler = new TestScheduler();

            var message = new ManagedMqttApplicationMessage();
            var @event = new ApplicationMessageProcessedEventArgs(message, new Exception());
            testScheduler.Schedule(TimeSpan.FromTicks(2), () =>
                mock.Mock<IManagedMqttClient>().Raise(m => m.ApplicationMessageProcessedAsync -= null, (object)@event));

            // act
            var testObserver = testScheduler.Start(() => rxMqttClient.ApplicationMessageProcessedEvent, 0, 0, 4);

            Assert.Equal(1, testObserver.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Last().Value.Kind);
            Assert.Equal(@event, testObserver.Messages.Last().Value.Value);
        }

        [Fact]
        public void ApplicationMessageSkippedHandler()
        {
            using var mock = AutoMock.GetLoose();
            var message = new ManagedMqttApplicationMessage();
            var @event = new ApplicationMessageSkippedEventArgs(message); ;

            var rxMqttClient = mock.Create<RxMqttClient>();

            var testScheduler = new TestScheduler();
            testScheduler.Schedule(TimeSpan.FromTicks(2), () => mock.Mock<IManagedMqttClient>().Raise(m => m.ApplicationMessageSkippedAsync += null, (object)@event));

            // act
            var testObserver = testScheduler.Start(() => rxMqttClient.ApplicationMessageSkippedEvent, 0, 0, 4);

            Assert.Equal(1, testObserver.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Last().Value.Kind);
            Assert.Equal(@event, testObserver.Messages.Last().Value.Value);
        }

        [Fact]
        public void Connected_Observer()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            var rxMqttClient = mock.Create<RxMqttClient>();

            var testScheduler = new TestScheduler();

            testScheduler.Schedule(TimeSpan.FromTicks(3), () =>
                mock.Mock<IManagedMqttClient>().Raise(x => x.ConnectedAsync += null, (object)new MqttClientConnectedEventArgs(new MqttClientConnectResult())));
            // act
            var testObserver = testScheduler.Start(() => rxMqttClient.Connected, 0, 0, 4);

            Assert.Equal(2, testObserver.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Last().Value.Kind);
            Assert.True(testObserver.Messages.Last().Value.Value);
            testScheduler.AdvanceBy(1);
        }

        [Fact]
        public void Connected_Returns_False_Init()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>();
            var rxMqttClient = mock.Create<RxMqttClient>();

            var testScheduler = new TestScheduler();

            // act
            var testObserver = testScheduler.Start(() => rxMqttClient.Connected, 0, 0, 1);

            Assert.Single(testObserver.Messages);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Single().Value.Kind);
            Assert.False(testObserver.Messages.Single().Value.Value);
        }

        [Fact]
        public void ConnectingFailedHandler()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            var rxMqttClient = mock.Create<RxMqttClient>();

            var testScheduler = new TestScheduler();

            var @event = new ConnectingFailedEventArgs(new MqttClientConnectResult(), new Exception());
            testScheduler.Schedule(TimeSpan.FromTicks(2), () => mock.Mock<IManagedMqttClient>()
                .Raise(x => x.ConnectingFailedAsync -= null, (object)@event));

            // act
            var testObserver = testScheduler.Start(() => rxMqttClient.ConnectingFailedEvent, 0, 0, 4);

            Assert.Equal(1, testObserver.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Last().Value.Kind);
            Assert.Equal(@event, testObserver.Messages.Last().Value.Value);
        }

        [Fact]
        public void CTOR_ArgumentNullException_Logger()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            var managedMqttClient = mock.Create<IManagedMqttClient>(); ;
            IMqttNetLogger logger = null;
            Assert.Throws<ArgumentNullException>(() => new RxMqttClient(managedMqttClient, logger));
        }

        [Fact]
        public void CTOR_ArgumentNullException_ManagedMqttClient()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IMqttNetLogger>();
            IManagedMqttClient managedMqttClient = null;
            var logger = mock.Create<IMqttNetLogger>();

            Assert.Throws<ArgumentNullException>(() => new RxMqttClient(managedMqttClient, logger));
        }

        [Fact]
        public void Disconnected_Observer()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            var rxMqttClient = mock.Create<RxMqttClient>();

            var testScheduler = new TestScheduler();

            testScheduler.Schedule(TimeSpan.FromTicks(2), () =>
                mock.Mock<IManagedMqttClient>().Raise(x => x.ConnectedAsync += null, (Func<MqttClientConnectedEventArgs, Task>)null));
            testScheduler.Schedule(TimeSpan.FromTicks(3), () =>
                mock.Mock<IManagedMqttClient>().Raise(x => x.DisconnectedAsync += null, (Func<MqttClientDisconnectedEventArgs, Task>)null));

            // act
            var testObserver = testScheduler.Start(() => rxMqttClient.Connected, 0, 1, 4);

            Assert.Equal(3, testObserver.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Last().Value.Kind);
            Assert.False(testObserver.Messages.Last().Value.Value);
        }

        [Fact]
        public void Dispose()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>()
                .Setup(x => x.Dispose())
                .Throws(new Exception());

            // act
            var rxMqttClient = mock.Create<RxMqttClient>();

            // test
            rxMqttClient.Dispose();
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
            var logger = new MqttNetEventLogger("MyCustomId");
            // act
            Assert.Throws<ArgumentNullException>(() => ((MqttFactory)null).CreateRxMqttClient(logger));
        }

        [Fact]
        public void Factory_With_Logger()
        {
            var logger = new MqttNetEventLogger("MyCustomId");
            // act
            var client = new MqttFactory().CreateRxMqttClient(logger);

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
        public async Task Options()
        {
            using var mock = AutoMock.GetLoose();

            var options = new ManagedMqttClientOptionsBuilder()
           .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
           .WithClientOptions(new MqttClientOptionsBuilder()
               .WithClientId("Client1")
               .WithTcpServer("broker.hivemq.com")
               .WithTlsOptions(_ => { })
               .Build())
           .Build();

            mock.Mock<IManagedMqttClient>()
                .Setup(x => x.Options)
                .Returns(options);
            var rxMqttClient = mock.Create<RxMqttClient>();

            // act
            await rxMqttClient.StartAsync(options);

            // test
            mock.Mock<IManagedMqttClient>().Verify(x => x.StartAsync(options));
            Assert.Equal(options, rxMqttClient.Options);
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
            var rxMqttClient = mock.Create<RxMqttClient>();

            // test
            Assert.Equal(count, rxMqttClient.PendingApplicationMessagesCount);
        }

        [Fact]
        public async Task PingAsync()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>();
            var rxMqttClient = mock.Create<RxMqttClient>();

            var cts = new CancellationToken();

            // act
            await rxMqttClient.PingAsync(cts);

            // test
            mock.Mock<IManagedMqttClient>().Verify(x => x.PingAsync(cts));
        }

        [Fact]
        public async Task PublishAsync()
        {
            using var mock = AutoMock.GetLoose();
            var message = new ManagedMqttApplicationMessage();

            mock.Mock<IManagedMqttClient>();
            var rxMqttClient = mock.Create<RxMqttClient>();

            // act
            await rxMqttClient.PublishAsync(message);

            // test
            mock.Mock<IManagedMqttClient>().Verify(x => x.EnqueueAsync(message));
        }

        [Fact]
        public void PublishAsync_ArgumentNullException()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            RxMqttClient rxMqttClient = mock.Create<RxMqttClient>();

            // act
            _ = Assert.ThrowsAsync<ArgumentNullException>(() =>
                rxMqttClient.PublishAsync((ManagedMqttApplicationMessage)null));
            _ = Assert.ThrowsAsync<ArgumentNullException>(() =>
                rxMqttClient.PublishAsync((MqttApplicationMessage)null));
        }

        [Fact]
        public async Task PublishAsync_CancellationToken()
        {
            using var mock = AutoMock.GetLoose();
            var message = new MqttApplicationMessage();

            mock.Mock<IManagedMqttClient>();
            var rxMqttClient = mock.Create<RxMqttClient>();

            // act
            await rxMqttClient.PublishAsync(message);

            // test
            mock.Mock<IManagedMqttClient>().Verify(x => x.EnqueueAsync(message));
        }

        [Fact]
        public void PublishAsync_CancellationToken_ArgumentNullException()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            var rxMqttClient = mock.Create<RxMqttClient>();

            // act
            _ = Assert.ThrowsAsync<ArgumentNullException>(() =>
                rxMqttClient.PublishAsync((ManagedMqttApplicationMessage)null));
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
            var rxMqttClient = mock.Create<RxMqttClient>();

            // test
            Assert.Equal(isStarted, rxMqttClient.IsConnected);
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
            var rxMqttClient = mock.Create<RxMqttClient>();

            // test
            Assert.Equal(isStarted, rxMqttClient.IsStarted);
        }

        [Fact]
        public async Task StartAsync()
        {
            using var mock = AutoMock.GetLoose();
            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId("Client1")
                    .WithTcpServer("broker.hivemq.com")
                    .WithTlsOptions(_ => { })
                    .Build())
                .Build();

            mock.Mock<IManagedMqttClient>();
            var rxMqttClient = mock.Create<RxMqttClient>();

            // act
            await rxMqttClient.StartAsync(options);

            // test
            mock.Mock<IManagedMqttClient>().Verify(x => x.StartAsync(options));
        }

        [Fact]
        public void StartAsync_ArgumentNullException()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            var rxMqttClient = mock.Create<RxMqttClient>();

            // act
            _ = Assert.ThrowsAsync<ArgumentNullException>(() => rxMqttClient.StartAsync(null));
        }

        [Fact]
        public async Task StopAsync()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>();
            var rxMqttClient = mock.Create<RxMqttClient>();

            // act
            await rxMqttClient.StopAsync();

            // test
            mock.Mock<IManagedMqttClient>().Verify(x => x.StopAsync(true));
        }

        [Fact]
        public void SynchronizingSubscriptionsFailedHandler()
        {
            using var mock = AutoMock.GetLoose();
            var rxMqttClient = mock.Create<RxMqttClient>();

            var testScheduler = new TestScheduler();

            var @event = new ManagedProcessFailedEventArgs(
                new Exception(),
                new List<MqttTopicFilter>(),
                new List<string>());

            testScheduler.Schedule(TimeSpan.FromTicks(2), () =>
                mock.Mock<IManagedMqttClient>().Raise(x => x.SynchronizingSubscriptionsFailedAsync += null, (object)@event));

            // act
            var testObserver = testScheduler.Start(() => rxMqttClient.SynchronizingSubscriptionsFailedEvent, 0, 0, 4);

            Assert.Equal(1, testObserver.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, testObserver.Messages.Last().Value.Kind);
            Assert.Equal(@event, testObserver.Messages.Last().Value.Value);
        }
    }
}