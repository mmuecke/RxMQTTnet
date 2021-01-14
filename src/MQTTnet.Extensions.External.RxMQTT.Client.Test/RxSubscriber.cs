using Autofac.Extras.Moq;
using Microsoft.Reactive.Testing;
using Moq;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MQTTnet.Extensions.External.RxMQTT.Client.Test
{
    public class RxSubscriber
    {
        [Theory]
        [InlineData(" ")]
        [InlineData("")]
        [InlineData(null)]
        public void Connect_ArguemntException(string topic)
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            var rxMqttClinet = mock.Create<RxMqttClient>();
            Assert.Throws<ArgumentException>(() => rxMqttClinet.Connect(topic));
        }

        [Fact]
        public void Connect_SubscribeAsync_Exception()
        {
            var exceptin = new Exception("Test");
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>().Setup(x => x.SubscribeAsync(It.IsAny<MqttTopicFilter[]>())).Throws(exceptin);
            mock.Mock<IMqttNetLogger>().Setup(x => x.CreateScopedLogger(It.IsAny<string>())).Returns(mock.Mock<IMqttNetScopedLogger>().Object);

            var rxMqttClinet = mock.Create<RxMqttClient>();
            var testScheduler = new TestScheduler();

            testScheduler.ScheduleAbsolute("Topic", 2, (_, state) => { rxMqttClinet.Connect(state); return Disposable.Empty; });

            var result = testScheduler.Start(() => rxMqttClinet.Connect("Topic"), 0, 0, 3);

            Assert.Single(result.Messages);
            Assert.Single(result.Messages.Where(record => record.Value.Kind == NotificationKind.OnError));
            Assert.Equal(exceptin, result.Messages.Where(record => record.Value.Kind == NotificationKind.OnError).Single().Value.Exception);
            mock.Mock<IMqttNetScopedLogger>().Verify(x => x.Publish(It.IsAny<MqttNetLogLevel>(), It.IsAny<string>(), It.IsAny<object[]>(), exceptin), Times.Once);
        }

        [Fact]
        public void Disconnect_UnsubscribeAsync_Exception()
        {
            var exceptin = new Exception("Test");
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>().Setup(x => x.SubscribeAsync(It.IsAny<MqttTopicFilter[]>())).Returns(Task.CompletedTask);
            mock.Mock<IManagedMqttClient>().Setup(x => x.UnsubscribeAsync(It.IsAny<string[]>())).Throws(exceptin);
            mock.Mock<IMqttNetLogger>().Setup(x => x.CreateScopedLogger(It.IsAny<string>())).Returns(mock.Mock<IMqttNetScopedLogger>().Object);

            var rxMqttClinet = mock.Create<RxMqttClient>();
            var testScheduler = new TestScheduler();

            testScheduler.ScheduleAbsolute("Topic", 2, (_, state) => { rxMqttClinet.Connect(state); return Disposable.Empty; });

            var result = testScheduler.Start(() => rxMqttClinet.Connect("Topic"), 0, 0, 3);

            Assert.Empty(result.Messages);
            mock.Mock<IMqttNetScopedLogger>().Verify(x => x.Publish(MqttNetLogLevel.Error, It.IsAny<string>(), It.IsAny<object[]>(), exceptin), Times.Once);
        }

        [Fact]
        public void Disconnect_UnsubscribeAsync_ObjectDisposedException_Leads_To_No_Error()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>().Setup(x => x.SubscribeAsync(It.IsAny<MqttTopicFilter[]>())).Returns(Task.CompletedTask);
            mock.Mock<IManagedMqttClient>().Setup(x => x.UnsubscribeAsync(It.IsAny<string[]>())).Throws(new ObjectDisposedException(nameof(IManagedMqttClient)));
            var rxMqttClinet = mock.Create<RxMqttClient>();
            var testScheduler = new TestScheduler();

            testScheduler.ScheduleAbsolute("Topic", 2, (_, state) => { rxMqttClinet.Connect(state); return Disposable.Empty; });

            var result = testScheduler.Start(() => rxMqttClinet.Connect("Topic"), 0, 0, 3);

            Assert.Empty(result.Messages);
            mock.Mock<IMqttNetScopedLogger>().Verify(x => x.Publish(MqttNetLogLevel.Error, It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<Exception>()), Times.Never);
        }

        [Fact]
        public void Publisch_2Subscribe_2Recive_1Dispose_1Recive_Dispose()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageReceivedHandler);

            var rxMqttClinet = mock.Create<RxMqttClient>();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("T")
                .WithPayload("P")
                .WithExactlyOnceQoS()
                .Build();
            var eventArgs = new MqttApplicationMessageReceivedEventArgs("1", message);
            var testScheduler = new TestScheduler();

            // act
            var firstCount = 0;
            var first = rxMqttClinet.Connect("T").Subscribe(_ => firstCount++);

            testScheduler.ScheduleAsync(TimeSpan.FromTicks(2), (_, __) => mock.Mock<IManagedMqttClient>().Object.ApplicationMessageReceivedHandler.HandleApplicationMessageReceivedAsync(eventArgs));
            testScheduler.ScheduleAbsolute(Unit.Default, 3, (_, __) =>
            {
                first.Dispose();
                return Disposable.Empty;
            });
            testScheduler.ScheduleAsync(TimeSpan.FromTicks(4), (_, __) => mock.Mock<IManagedMqttClient>().Object.ApplicationMessageReceivedHandler.HandleApplicationMessageReceivedAsync(eventArgs));

            var result = testScheduler.Start(() =>
            {
                IObservable<MqttApplicationMessageReceivedEventArgs> first = rxMqttClinet.Connect("T");
                return rxMqttClinet.Connect("T");
            }, 0, 0, 5);

            // test
            Assert.Equal(2, result.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, result.Messages.First().Value.Kind);
            Assert.Equal(NotificationKind.OnNext, result.Messages.Last().Value.Kind);
            Assert.Equal(eventArgs, result.Messages.First().Value.Value);
            Assert.Equal(eventArgs, result.Messages.Last().Value.Value);
            Assert.Null(mock.Mock<IManagedMqttClient>().Object.ApplicationMessageReceivedHandler);
            Assert.Equal(1, firstCount);
        }

        [Fact]
        public void Publisch_Subscribe_Once_And_2Recive_Dispose()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageReceivedHandler);

            var rxMqttClinet = mock.Create<RxMqttClient>();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("T")
                .WithPayload("P")
                .WithExactlyOnceQoS()
                .Build();
            var eventArgs = new MqttApplicationMessageReceivedEventArgs("1", message);
            var testScheduler = new TestScheduler();

            // act
            testScheduler.ScheduleAsync(TimeSpan.FromTicks(2), (_, __) => mock.Mock<IManagedMqttClient>().Object.ApplicationMessageReceivedHandler.HandleApplicationMessageReceivedAsync(eventArgs));
            testScheduler.ScheduleAsync(TimeSpan.FromTicks(3), (_, __) => mock.Mock<IManagedMqttClient>().Object.ApplicationMessageReceivedHandler.HandleApplicationMessageReceivedAsync(eventArgs));
            var result = testScheduler.Start(() => rxMqttClinet.Connect("T"), 0, 0, 4);

            // test
            Assert.Equal(2, result.Messages.Count);
            Assert.Equal(NotificationKind.OnNext, result.Messages.First().Value.Kind);
            Assert.Equal(NotificationKind.OnNext, result.Messages.Last().Value.Kind);
            Assert.Equal(eventArgs, result.Messages.First().Value.Value);
            Assert.Equal(eventArgs, result.Messages.Last().Value.Value);
            Assert.Null(mock.Mock<IManagedMqttClient>().Object.ApplicationMessageReceivedHandler);
        }

        [Fact]
        public void Publisch_Subscribe_Once_And_Dispose_Client()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageReceivedHandler);

            var rxMqttClinet = mock.Create<RxMqttClient>();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("T")
                .WithPayload("P")
                .WithExactlyOnceQoS()
                .Build();
            var eventArgs = new MqttApplicationMessageReceivedEventArgs("1", message);
            var testScheduler = new TestScheduler();

            // act
            testScheduler.ScheduleAbsolute<Unit>(Unit.Default, 3, (_, __) => { rxMqttClinet.Dispose(); return Disposable.Empty; }); ;
            var result = testScheduler.Start(() => rxMqttClinet.Connect("T"), 0, 0, 4);

            // test
            Assert.Single(result.Messages);
            Assert.Equal(NotificationKind.OnCompleted, result.Messages.Single().Value.Kind);
            Assert.Null(mock.Mock<IManagedMqttClient>().Object.ApplicationMessageReceivedHandler);
        }

        [Fact]
        public void Publisch_Subscribe_Once_And_NotReciveDueFilter_Dispose()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageReceivedHandler);

            var rxMqttClinet = mock.Create<RxMqttClient>();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("N")
                .WithPayload("P")
                .WithExactlyOnceQoS()
                .Build();
            var eventArgs = new MqttApplicationMessageReceivedEventArgs("1", message);
            var testScheduler = new TestScheduler();

            // act
            testScheduler.ScheduleAsync(TimeSpan.FromTicks(2), (_, __) => mock.Mock<IManagedMqttClient>().Object.ApplicationMessageReceivedHandler.HandleApplicationMessageReceivedAsync(eventArgs));
            var result = testScheduler.Start(() => rxMqttClinet.Connect("T"), 0, 0, 3);

            // test
            Assert.Empty(result.Messages);
            Assert.Null(mock.Mock<IManagedMqttClient>().Object.ApplicationMessageReceivedHandler);
        }

        [Fact]
        public void Publisch_Subscribe_Once_And_Recive_Dispose()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageReceivedHandler);

            var rxMqttClinet = mock.Create<RxMqttClient>();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("T")
                .WithPayload("P")
                .WithExactlyOnceQoS()
                .Build();
            var eventArgs = new MqttApplicationMessageReceivedEventArgs("1", message);
            var testScheduler = new TestScheduler();

            // act
            testScheduler.ScheduleAsync(TimeSpan.FromTicks(2), (_, __) => mock.Mock<IManagedMqttClient>().Object.ApplicationMessageReceivedHandler.HandleApplicationMessageReceivedAsync(eventArgs));
            var result = testScheduler.Start(() => rxMqttClinet.Connect("T"), 0, 0, 3);

            // test
            Assert.Single(result.Messages);
            Assert.Equal(NotificationKind.OnNext, result.Messages.Single().Value.Kind);
            Assert.Equal(eventArgs, result.Messages.Single().Value.Value);
            Assert.Null(mock.Mock<IManagedMqttClient>().Object.ApplicationMessageReceivedHandler);
        }

        [Fact]
        public async void PublishAsync()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            var rxMqttClinet = mock.Create<RxMqttClient>();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("T")
                .WithPayload("P")
                .WithExactlyOnceQoS()
                .Build();

            var mangedMessage = new ManagedMqttApplicationMessageBuilder()
                .WithApplicationMessage(message)
                .Build();

            // act
            await rxMqttClinet.PublishAsync(mangedMessage);

            // test
            mock.Mock<IManagedMqttClient>().Verify(x => x.PublishAsync(mangedMessage));
        }
    }
}