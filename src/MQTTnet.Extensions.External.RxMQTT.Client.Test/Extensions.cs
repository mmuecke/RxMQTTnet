using Autofac.Extras.Moq;
using Microsoft.Reactive.Testing;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using Xunit;

namespace MQTTnet.Extensions.External.RxMQTT.Client.Test
{
    public class Extensions
    {
        [Theory]
        [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, true)]
        [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, false)]
        public void FilterQoS_ApplicationEvent(MqttQualityOfServiceLevel filter, bool success)
        {
            using var mock = AutoMock.GetLoose();
            var message = new MqttApplicationMessageBuilder()
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                .WithTopic("T")
                .WithPayload("P")
                .Build();
            var @event = new MqttApplicationMessageReceivedEventArgs("C", message, mock.Create<MqttPublishPacket>(), null);

            var observable = Observable.Return(@event).FilterQoS(filter);

            var testScheduler = new TestScheduler();

            var observableResult = testScheduler.Start(() => observable, 0, 0, 1);

            Assert.Equal(success, observableResult.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).Any());
        }

        [Fact]
        public void FilterQoS_ApplicationEvent_ArgumentNullException()
        {
            IObservable<MqttApplicationMessageReceivedEventArgs> observable = null;
            Assert.Throws<ArgumentNullException>(() => observable.FilterQoS(MqttQualityOfServiceLevel.ExactlyOnce));
        }

        [Theory]
        [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, true)]
        [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, false)]
        public void FilterQoS_ApplicationMessage(MqttQualityOfServiceLevel filter, bool success)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                .WithTopic("T")
                .WithPayload("P")
                .Build();

            var observable = Observable.Return(message).FilterQoS(filter);

            var testScheduler = new TestScheduler();

            var observableResult = testScheduler.Start(() => observable, 0, 0, 1);

            Assert.Equal(success, observableResult.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).Any());
        }

        [Fact]
        public void FilterQoS_ApplicationMessage_ArgumentNullException()
        {
            IObservable<MqttApplicationMessage> observable = null;
            Assert.Throws<ArgumentNullException>(() => observable.FilterQoS(MqttQualityOfServiceLevel.ExactlyOnce));
        }

        [Theory]
        [InlineData("T", true)]
        [InlineData("N", false)]
        public void FilterTopic_ApplicationEvent(string filter, bool success)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                .WithTopic("T")
                .WithPayload("P")
                .Build();

            var observable = Observable.Return(message).FilterTopic(filter);

            var testScheduler = new TestScheduler();

            var observableResult = testScheduler.Start(() => observable, 0, 0, 1);

            Assert.Equal(success, observableResult.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).Any());
        }

        [Fact]
        public void FilterTopic_ApplicationEvent_ArgumentNullException()
        {
            IObservable<MqttApplicationMessageReceivedEventArgs> observable = null;
            Assert.Throws<ArgumentNullException>(() => observable.FilterTopic("Filter"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void FilterTopic_ApplicationEvent_ArgumentNullException_Filter(string topic)
        {
            Assert.Throws<ArgumentException>(() => Observable.Never<MqttApplicationMessageReceivedEventArgs>().FilterTopic(topic));
        }

        [Theory]
        [InlineData("T", true)]
        [InlineData("N", false)]
        public void FilterTopic_ApplicationMessage(string filter, bool success)
        {
            using var mock = AutoMock.GetLoose();
            var message = new MqttApplicationMessageBuilder()
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                .WithTopic("T")
                .WithPayload("P")
                .Build();
            var @event = new MqttApplicationMessageReceivedEventArgs("C", message, mock.Create<MqttPublishPacket>(), null);

            var observable = Observable.Return(@event).FilterTopic(filter);

            var testScheduler = new TestScheduler();

            var observableResult = testScheduler.Start(() => observable, 0, 0, 1);

            Assert.Equal(success, observableResult.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).Any());
        }

        [Fact]
        public void FilterTopic_ApplicationMessage_ArgumentNullException()
        {
            IObservable<MqttApplicationMessage> observable = null;
            Assert.Throws<ArgumentNullException>(() => observable.FilterTopic("Filter"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void FilterTopic_ApplicationMessage_ArgumentNullException_Filter(string topic)
        {
            Assert.Throws<ArgumentException>(() => Observable.Never<MqttApplicationMessage>().FilterTopic(topic));
        }

        [Fact]
        public void GetPayload()
        {
            using var mock = AutoMock.GetLoose();
            var message = new MqttApplicationMessageBuilder()
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                .WithTopic("T")
                .WithPayload("P")
                .Build();
            var @event = new MqttApplicationMessageReceivedEventArgs("C", message, mock.Create<MqttPublishPacket>(), null);

            var observable = Observable.Return(@event).SelectPayload();

            var testScheduler = new TestScheduler();

            var observableResult = testScheduler.Start(() => observable, 0, 0, 1);

            Assert.Equal("P", observableResult.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).First().Value.Value);
        }

        [Fact]
        public void GetPayload_T_FromEvent()
        {
            using var mock = AutoMock.GetLoose();
            var message = new MqttApplicationMessageBuilder()
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                .WithTopic("T")
                .WithPayload("P")
                .Build();
            var @event = new MqttApplicationMessageReceivedEventArgs("C", message, mock.Create<MqttPublishPacket>(), null);

            var observable = Observable.Return(@event).SelectPayload(p => p);

            var testScheduler = new TestScheduler();

            var observableResult = testScheduler.Start(() => observable, 0, 0, 1);

            Assert.Equal(message.PayloadSegment, observableResult.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).First().Value.Value);
        }

        [Fact]
        public void SelectMessage()
        {
            using var mock = AutoMock.GetLoose();
            var message = new MqttApplicationMessageBuilder()
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                .WithTopic("T")
                .WithPayload("P")
                .Build();
            var @event = new MqttApplicationMessageReceivedEventArgs("C", message, mock.Create<MqttPublishPacket>(), null);

            var observable = Observable.Return(@event).SelectMessage();

            var testScheduler = new TestScheduler();

            var observableResult = testScheduler.Start(() => observable, 0, 0, 1);

            Assert.Equal(message, observableResult.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).First().Value.Value);
        }

        [Fact]
        public void SelectMessage_ArgumentNullException()
        {
            IObservable<MqttApplicationMessageReceivedEventArgs> observable = null;

            Assert.Throws<ArgumentNullException>(() => observable.SelectMessage());
        }

        [Fact]
        public void SelectPayload_FromEvent_ArgumentNullException()
        {
            IObservable<MqttApplicationMessageReceivedEventArgs> observable = null;

            Assert.Throws<ArgumentNullException>(() => observable.SelectPayload());
        }

        [Fact]
        public void SelectPayload_FromEvent_T_ArgumentNullException()
        {
            IObservable<MqttApplicationMessageReceivedEventArgs> observable = null;

            Assert.Throws<ArgumentNullException>(() => observable.SelectPayload(p => p));
        }

        [Fact]
        public void SelectPayload_FromEvent_T_ArgumentNullException_GetPayloadFunc()
        {
            Func<byte[], byte[]> getPayloadFunc = null;

            Assert.Throws<ArgumentNullException>(() => Observable.Never<MqttApplicationMessageReceivedEventArgs>().SelectPayload(getPayloadFunc));
        }

        [Fact]
        public void SelectPayload_FromMessage_ArgumentNullException()
        {
            IObservable<MqttApplicationMessage> observable = null;

            Assert.Throws<ArgumentNullException>(() => observable.SelectPayload());
        }

        [Fact]
        public void SelectPayload_FromMessage_T_ArgumentNullException()
        {
            IObservable<MqttApplicationMessage> observable = null;

            Assert.Throws<ArgumentNullException>(() => observable.SelectPayload(p => p));
        }

        [Fact]
        public void SelectPayload_FromMessage_T_ArgumentNullException_GetPayloadFunc()
        {
            Func<byte[], byte[]> getPayloadFunc = null;

            Assert.Throws<ArgumentNullException>(() => Observable.Never<MqttApplicationMessage>().SelectPayload(getPayloadFunc));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SelectPayload_T_FromEvent_Exception(bool skipOnError)
        {
            using var mock = AutoMock.GetLoose();
            var message = new MqttApplicationMessageBuilder()
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                .WithTopic("T")
                .WithPayload("P")
                .Build();
            var @event = new MqttApplicationMessageReceivedEventArgs("C", message, mock.Create<MqttPublishPacket>(), null);
            var ex = new Exception();
            var observable = Observable.Return(@event).SelectPayload<byte[]>(p => throw ex, skipOnError);

            var testScheduler = new TestScheduler();

            var observableResult = testScheduler.Start(() => observable, 0, 0, 1);

            if (skipOnError)
                Assert.Single(observableResult.Messages);
            else
                Assert.Equal(ex, observableResult.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnError).First().Value.Exception);
        }

        [Fact]
        public void SelectPayload_T_FromEvent_SourceException()
        {
            var observable = Observable
                .Create<MqttApplicationMessageReceivedEventArgs>(o => { o.OnError(new Exception()); return Disposable.Empty; })
                .SelectPayload(p => p);

            var testScheduler = new TestScheduler();

            var observableResult = testScheduler.Start(() => observable, 0, 0, 1);

            Assert.Single(observableResult.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnError));
        }

        [Fact]
        public void ToUTF8String()
        {
            var payload = "Test";
            var array = Encoding.UTF8.GetBytes(payload);
            var result = array.ToUTF8String("");
            Assert.Equal(payload, result);
        }

        [Fact]
        public void ToUTF8String_Default()
        {
            var defaultPayload = "Test";
            byte[] array = null;
            var result = array.ToUTF8String(defaultPayload);
            Assert.Equal(defaultPayload, result);
        }
    }
}