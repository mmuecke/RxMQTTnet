using Autofac.Extras.Moq;
using Microsoft.Reactive.Testing;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Xunit;

namespace MQTTnet.Extensions.External.RxMQTT.Client.Test
{
    public class RxSubscriber
    {
        [Fact]
        public void Publisch_2Subscribe_2Recive_1Dispose_1Recive_Dispose()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageReceivedHandler);

            var rxMqttClinet = mock.Create<RxMqttClinet>();

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

            var rxMqttClinet = mock.Create<RxMqttClinet>();

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

            var rxMqttClinet = mock.Create<RxMqttClinet>();

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

            var rxMqttClinet = mock.Create<RxMqttClinet>();

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

            var rxMqttClinet = mock.Create<RxMqttClinet>();

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
            var rxMqttClinet = mock.Create<RxMqttClinet>();

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