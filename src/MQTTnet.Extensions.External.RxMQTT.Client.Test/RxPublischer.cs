using Autofac.Extras.Moq;
using Microsoft.Reactive.Testing;
using Moq;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MQTTnet.Extensions.External.RxMQTT.Client.Test
{
    public class RxPublischer
    {
        [Fact]
        public void Publish_HasFailed()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>()
                .Setup(x => x.PublishAsync(It.IsAny<ManagedMqttApplicationMessage>())).Returns(Task.CompletedTask);
            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageProcessedHandler);
            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageSkippedHandler);
            mock.Mock<IManagedMqttClient>()
                .SetupGet(x => x.IsConnected)
                .Returns(true);

            var rxMqttClinet = mock.Create<RxMqttClinet>();

            var message = new ManagedMqttApplicationMessageBuilder()
                .WithApplicationMessage(new MqttApplicationMessageBuilder()
                    .WithTopic("T")
                    .WithPayload("P")
                    .WithExactlyOnceQoS()
                    .Build())
                .Build();
            var observable = Observable.Return(message);
            var exception = new Exception();
            var @event = new ApplicationMessageProcessedEventArgs(message, exception);

            var testScheduler = new TestScheduler();
            testScheduler.ScheduleAbsolute(@event, 5, (_, state) =>
            {
                var IManagedMqttClientMock = mock.Mock<IManagedMqttClient>().Object;
                mock.Mock<IManagedMqttClient>().Object.ApplicationMessageProcessedHandler.HandleApplicationMessageProcessedAsync(state);
                return Disposable.Empty;
            });
            // act
            var testObserver = testScheduler.Start(() => observable.PublishOn(rxMqttClinet), 0, 1, 10);

            Assert.Single(testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext));
            Assert.Single(testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnCompleted));
            Assert.Empty(testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnError));
            var onNext = testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).Single();
            Assert.Equal(message, onNext.Value.Value.MqttApplicationMessage);
            Assert.Equal(RxMqttClientPublishReasonCode.HasFailed, onNext.Value.Value.ReasonCode);
            Assert.Equal(exception, onNext.Value.Value.Exception);
        }

        [Fact]
        public void Publish_HasSkipped()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>()
                .Setup(x => x.PublishAsync(It.IsAny<ManagedMqttApplicationMessage>())).Returns(Task.CompletedTask);
            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageProcessedHandler);
            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageSkippedHandler);
            mock.Mock<IManagedMqttClient>()
                .SetupGet(x => x.IsConnected)
                .Returns(true);

            var rxMqttClinet = mock.Create<RxMqttClinet>();

            var message = new ManagedMqttApplicationMessageBuilder()
                .WithApplicationMessage(new MqttApplicationMessageBuilder()
                    .WithTopic("T")
                    .WithPayload("P")
                    .WithExactlyOnceQoS()
                    .Build())
                .Build();
            var observable = Observable.Return(message);
            var @event = new ApplicationMessageSkippedEventArgs(message);

            var testScheduler = new TestScheduler();
            testScheduler.ScheduleAbsolute(@event, 5, (_, state) =>
            {
                var IManagedMqttClientMock = mock.Mock<IManagedMqttClient>().Object;
                mock.Mock<IManagedMqttClient>().Object.ApplicationMessageSkippedHandler.HandleApplicationMessageSkippedAsync(state);
                return Disposable.Empty;
            });
            // act
            var testObserver = testScheduler.Start(() => observable.PublishOn(rxMqttClinet), 0, 1, 10);

            Assert.Single(testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext));
            Assert.Single(testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnCompleted));
            Assert.Empty(testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnError));
            var onNext = testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).Single();
            Assert.Equal(message, onNext.Value.Value.MqttApplicationMessage);
            Assert.Equal(RxMqttClientPublishReasonCode.HasSkipped, onNext.Value.Value.ReasonCode);
            Assert.Null(onNext.Value.Value.Exception);
        }

        [Fact]
        public void Publish_HasSucceeded()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>()
                .Setup(x => x.PublishAsync(It.IsAny<ManagedMqttApplicationMessage>())).Returns(Task.CompletedTask);
            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageProcessedHandler);
            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageSkippedHandler);
            mock.Mock<IManagedMqttClient>()
                .SetupGet(x => x.IsConnected)
                .Returns(true);

            var rxMqttClinet = mock.Create<RxMqttClinet>();

            var message = new ManagedMqttApplicationMessageBuilder()
                .WithApplicationMessage(new MqttApplicationMessageBuilder()
                    .WithTopic("T")
                    .WithPayload("P")
                    .WithExactlyOnceQoS()
                    .Build())
                .Build();
            var observable = Observable.Return(message);
            var @event = new ApplicationMessageProcessedEventArgs(message, null);

            var testScheduler = new TestScheduler();
            testScheduler.ScheduleAbsolute(@event, 5, (_, state) =>
            {
                var IManagedMqttClientMock = mock.Mock<IManagedMqttClient>().Object;
                mock.Mock<IManagedMqttClient>().Object.ApplicationMessageProcessedHandler.HandleApplicationMessageProcessedAsync(state);
                return Disposable.Empty;
            });
            // act
            var testObserver = testScheduler.Start(() => observable.PublishOn(rxMqttClinet), 0, 1, 10);

            Assert.Single(testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext));
            Assert.Single(testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnCompleted));
            Assert.Empty(testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnError));
            var onNext = testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).Single();
            Assert.Equal(message, onNext.Value.Value.MqttApplicationMessage);
            Assert.Equal(RxMqttClientPublishReasonCode.HasSucceeded, onNext.Value.Value.ReasonCode);
            Assert.Null(onNext.Value.Value.Exception);
        }

        [Fact]
        public void Publish_Internal_Method_Throws()
        {
            using var mock = AutoMock.GetLoose();

            var exception = new Exception();
            mock.Mock<IManagedMqttClient>()
                .Setup(x => x.PublishAsync(It.IsAny<ManagedMqttApplicationMessage>())).Throws(exception);
            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageProcessedHandler);
            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageSkippedHandler);
            mock.Mock<IManagedMqttClient>()
                .SetupGet(x => x.IsConnected)
                .Returns(true);

            var rxMqttClinet = mock.Create<RxMqttClinet>();

            var message = new ManagedMqttApplicationMessageBuilder()
                .WithApplicationMessage(new MqttApplicationMessageBuilder()
                    .WithTopic("T")
                    .WithPayload("P")
                    .WithExactlyOnceQoS()
                    .Build())
                .Build();
            var observable = Observable.Return(message);
            var @event = new ApplicationMessageProcessedEventArgs(message, exception);

            var testScheduler = new TestScheduler();
            testScheduler.ScheduleAbsolute(@event, 5, (_, state) =>
            {
                var IManagedMqttClientMock = mock.Mock<IManagedMqttClient>().Object;
                mock.Mock<IManagedMqttClient>().Object.ApplicationMessageProcessedHandler.HandleApplicationMessageProcessedAsync(state);
                return Disposable.Empty;
            });
            // act
            var testObserver = testScheduler.Start(() => observable.PublishOn(rxMqttClinet), 0, 1, 10);

            Assert.Single(testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext));
            Assert.Single(testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnCompleted));
            Assert.Empty(testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnError));
            var onNext = testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).Single();
            Assert.Equal(message, onNext.Value.Value.MqttApplicationMessage);
            Assert.Equal(RxMqttClientPublishReasonCode.HasFailed, onNext.Value.Value.ReasonCode);
            Assert.Equal(exception, onNext.Value.Value.Exception);
        }

        [Fact]
        public void Publish_ManagedMqttApplicationMessage()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IRxMqttClinet>().Setup(x => x.PublishAsync(It.IsAny<ManagedMqttApplicationMessage>())).Returns(Task.CompletedTask);
            mock.Mock<IRxMqttClinet>().Setup(x => x.ApplicationMessageProcessedEvent).Returns(Observable.Never<ApplicationMessageProcessedEventArgs>());
            mock.Mock<IRxMqttClinet>().Setup(x => x.ApplicationMessageSkippedEvent).Returns(Observable.Never<ApplicationMessageSkippedEventArgs>());
            mock.Mock<IRxMqttClinet>().Setup(x => x.IsConnected).Returns(true);
            var rxMqttClinet = mock.Mock<IRxMqttClinet>();

            var message = new ManagedMqttApplicationMessageBuilder()
                .WithApplicationMessage(new MqttApplicationMessageBuilder()
                    .WithTopic("T")
                    .WithPayload("P")
                    .WithExactlyOnceQoS()
                    .Build())
                .Build();
            var observable = Observable.Return(message);

            // act
            rxMqttClinet.Object.Publish(observable).Subscribe();

            // test
            rxMqttClinet.Verify(x => x.PublishAsync(message));
        }

        [Fact]
        public void Publish_ManagedMqttApplicationMessage_ArgumentNullException()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            var rxMqttClinet = mock.Create<RxMqttClinet>();
            IObservable<ManagedMqttApplicationMessage> observable = null;

            // test
            Assert.Throws<ArgumentNullException>(() => rxMqttClinet.Publish(observable));
        }

        [Fact]
        public void Publish_ManagedMqttApplicationMessage_Cient_ArgumentNullException()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            RxMqttClinet rxMqttClinet = null;
            var observable = Observable.Never<ManagedMqttApplicationMessage>();

            // test
            Assert.Throws<ArgumentNullException>(() => rxMqttClinet.Publish(observable));
        }

        [Fact]
        public void Publish_MqttApplicationMessage()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IRxMqttClinet>().Setup(x => x.PublishAsync(It.IsAny<ManagedMqttApplicationMessage>())).Returns(Task.CompletedTask);
            mock.Mock<IRxMqttClinet>().Setup(x => x.ApplicationMessageProcessedEvent).Returns(Observable.Never<ApplicationMessageProcessedEventArgs>());
            mock.Mock<IRxMqttClinet>().Setup(x => x.ApplicationMessageSkippedEvent).Returns(Observable.Never<ApplicationMessageSkippedEventArgs>());
            mock.Mock<IRxMqttClinet>().Setup(x => x.IsConnected).Returns(true);
            var rxMqttClinet = mock.Mock<IRxMqttClinet>();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("T")
                .WithPayload("P")
                .WithExactlyOnceQoS()
                .Build();

            var observable = Observable.Return(message);

            // act
            rxMqttClinet.Object.Publish(observable).Subscribe();

            // test
            rxMqttClinet.Verify(x => x.PublishAsync(It.IsAny<ManagedMqttApplicationMessage>()));
        }

        [Fact]
        public void Publish_MqttApplicationMessage_ArgumentNullException()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            var rxMqttClinet = mock.Create<RxMqttClinet>();
            IObservable<MqttApplicationMessage> observable = null;

            // test
            Assert.Throws<ArgumentNullException>(() => rxMqttClinet.Publish(observable));
        }

        [Fact]
        public void Publish_MqttApplicationMessage_Cient_ArgumentNullException()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            RxMqttClinet rxMqttClinet = null;
            var observable = Observable.Never<MqttApplicationMessage>();

            // test
            Assert.Throws<ArgumentNullException>(() => rxMqttClinet.Publish(observable));
        }

        [Fact]
        public void Publish_Not_IsConnected()
        {
            using var mock = AutoMock.GetLoose();

            mock.Mock<IManagedMqttClient>()
                .Setup(x => x.PublishAsync(It.IsAny<ManagedMqttApplicationMessage>())).Returns(Task.CompletedTask);
            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageProcessedHandler);
            mock.Mock<IManagedMqttClient>()
                .SetupProperty(x => x.ApplicationMessageSkippedHandler);

            var rxMqttClinet = mock.Create<RxMqttClinet>();

            var message = new ManagedMqttApplicationMessageBuilder()
                .WithApplicationMessage(new MqttApplicationMessageBuilder()
                    .WithTopic("T")
                    .WithPayload("P")
                    .WithExactlyOnceQoS()
                    .Build())
                .Build();
            var observable = Observable.Return(message);
            var @event = new ApplicationMessageProcessedEventArgs(message, null);

            var testScheduler = new TestScheduler();
            // act
            var testObserver = testScheduler.Start(() => observable.PublishOn(rxMqttClinet), 0, 0, 2);

            Assert.Single(testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext));
            Assert.Single(testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnCompleted));
            Assert.Empty(testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnError));
            var onNext = testObserver.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).Single();
            Assert.Equal(message, onNext.Value.Value.MqttApplicationMessage);
            Assert.Equal(RxMqttClientPublishReasonCode.ClientNotConnected, onNext.Value.Value.ReasonCode);
            Assert.Null(onNext.Value.Value.Exception);
        }

        [Fact]
        public void PublishOn_ManagedMqttApplicationMessage_ArgumentNullException()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            var rxMqttClinet = mock.Create<RxMqttClinet>();
            IObservable<ManagedMqttApplicationMessage> observable = null;

            // test
            Assert.Throws<ArgumentNullException>(() => observable.PublishOn(rxMqttClinet));
        }

        [Fact]
        public void PublishOn_ManagedMqttApplicationMessage_Cient_ArgumentNullException()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            RxMqttClinet rxMqttClinet = null;
            var observable = Observable.Never<ManagedMqttApplicationMessage>();

            // test
            Assert.Throws<ArgumentNullException>(() => observable.PublishOn(rxMqttClinet));
        }

        [Fact]
        public void PublishOn_MqttApplicationMessage()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IRxMqttClinet>().Setup(x => x.PublishAsync(It.IsAny<ManagedMqttApplicationMessage>())).Returns(Task.CompletedTask);
            mock.Mock<IRxMqttClinet>().Setup(x => x.ApplicationMessageProcessedEvent).Returns(Observable.Never<ApplicationMessageProcessedEventArgs>());
            mock.Mock<IRxMqttClinet>().Setup(x => x.ApplicationMessageSkippedEvent).Returns(Observable.Never<ApplicationMessageSkippedEventArgs>());
            mock.Mock<IRxMqttClinet>().Setup(x => x.IsConnected).Returns(true);
            var rxMqttClinet = mock.Mock<IRxMqttClinet>();

            var message = new MqttApplicationMessageBuilder()
                .WithTopic("T")
                .WithPayload("P")
                .WithExactlyOnceQoS()
                .Build();

            var observable = Observable.Return(message);

            // act
            observable.PublishOn(rxMqttClinet.Object).Subscribe();

            // test
            rxMqttClinet.Verify(x => x.PublishAsync(It.IsAny<ManagedMqttApplicationMessage>()));
        }

        [Fact]
        public void PublishOn_MqttApplicationMessage_ArgumentNullException()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            var rxMqttClinet = mock.Create<RxMqttClinet>();
            IObservable<MqttApplicationMessage> observable = null;

            // test
            Assert.Throws<ArgumentNullException>(() => observable.PublishOn(rxMqttClinet));
        }

        [Fact]
        public void PublishOn_MqttApplicationMessage_Cient_ArgumentNullException()
        {
            using var mock = AutoMock.GetLoose();
            mock.Mock<IManagedMqttClient>();
            RxMqttClinet rxMqttClinet = null;
            var observable = Observable.Never<MqttApplicationMessage>();

            // test
            Assert.Throws<ArgumentNullException>(() => observable.PublishOn(rxMqttClinet));
        }
    }
}