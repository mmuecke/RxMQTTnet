using Microsoft.Reactive.Testing;
using MQTTnet;
using MQTTnet.Protocol;
using System;
using System.Linq;
using System.Reactive.Linq;
using Xunit;

namespace MQTTnet.Extensions.RxMQTTnet.Test
{
    public class Extensions
    {
        [Theory]
        [InlineData("T", true)]
        [InlineData("N", false)]
        public void FilterTopicApplicationMessage(string filter, bool success)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithExactlyOnceQoS()
                .WithTopic("T")
                .WithPayload("P")
                .Build();
            var @event = new MqttApplicationMessageReceivedEventArgs("C", message);

            var observable = Observable.Return(@event).FilterTopic(filter);

            var testScheduler = new TestScheduler();

            var observableResult = testScheduler.Start(() => observable, 0, 0, 1);

            Assert.Equal(success, observableResult.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).Any());
        }

        [Theory]
        [InlineData("T", true)]
        [InlineData("N", false)]
        public void FilterTopicApplicationEvent(string filter, bool success)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithExactlyOnceQoS()
                .WithTopic("T")
                .WithPayload("P")
                .Build();

            var observable = Observable.Return(message).FilterTopic(filter);

            var testScheduler = new TestScheduler();

            var observableResult = testScheduler.Start(() => observable, 0, 0, 1);

            Assert.Equal(success, observableResult.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).Any());
        }

        [Theory]
        [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, true)]
        [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, false)]
        public void FilterQoSApplicationMessage(MqttQualityOfServiceLevel filter, bool success)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithExactlyOnceQoS()
                .WithTopic("T")
                .WithPayload("P")
                .Build();

            var observable = Observable.Return(message).FilterQoS(filter);

            var testScheduler = new TestScheduler();

            var observableResult = testScheduler.Start(() => observable, 0, 0, 1);

            Assert.Equal(success, observableResult.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).Any());
        }

        [Theory]
        [InlineData(MqttQualityOfServiceLevel.ExactlyOnce, true)]
        [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, false)]
        public void FilterQoSEvent(MqttQualityOfServiceLevel filter, bool success)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithExactlyOnceQoS()
                .WithTopic("T")
                .WithPayload("P")
                .Build();
            var @event = new MqttApplicationMessageReceivedEventArgs("C", message);

            var observable = Observable.Return(@event).FilterQoS(filter);

            var testScheduler = new TestScheduler();

            var observableResult = testScheduler.Start(() => observable, 0, 0, 1);

            Assert.Equal(success, observableResult.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).Any());
        }

        [Fact]
        public void GetMessage()
        {
            var message = new MqttApplicationMessageBuilder()
                .WithExactlyOnceQoS()
                .WithTopic("T")
                .WithPayload("P")
                .Build();
            var @event = new MqttApplicationMessageReceivedEventArgs("C", message);

            var observable = Observable.Return(@event).SelectMessage();

            var testScheduler = new TestScheduler();

            var observableResult = testScheduler.Start(() => observable, 0, 0, 1);

            Assert.Equal(message, observableResult.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).First().Value.Value);
        }

        [Fact]
        public void GetPayload()
        {
            var message = new MqttApplicationMessageBuilder()
                .WithExactlyOnceQoS()
                .WithTopic("T")
                .WithPayload("P")
                .Build();
            var @event = new MqttApplicationMessageReceivedEventArgs("C", message);

            var observable = Observable.Return(@event).GetPayload();

            var testScheduler = new TestScheduler();

            var observableResult = testScheduler.Start(() => observable, 0, 0, 1);

            Assert.Equal("P", observableResult.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).First().Value.Value);
        }

        [Fact]
        public void GetPayloadT()
        {
            var message = new MqttApplicationMessageBuilder()
                .WithExactlyOnceQoS()
                .WithTopic("T")
                .WithPayload("P")
                .Build();
            var @event = new MqttApplicationMessageReceivedEventArgs("C", message);

            var observable = Observable.Return(@event).GetPayload(p => p);

            var testScheduler = new TestScheduler();

            var observableResult = testScheduler.Start(() => observable, 0, 0, 1);

            Assert.Equal(message.Payload, observableResult.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnNext).First().Value.Value);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetPayloadT_Exception(bool skipOnError)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithExactlyOnceQoS()
                .WithTopic("T")
                .WithPayload("P")
                .Build();
            var @event = new MqttApplicationMessageReceivedEventArgs("C", message);
            var ex = new Exception();
            var observable = Observable.Return(@event).GetPayload<byte[]>(p => throw ex, skipOnError);

            var testScheduler = new TestScheduler();

            var observableResult = testScheduler.Start(() => observable, 0, 0, 1);

            if (skipOnError)
                Assert.Single(observableResult.Messages);
            else
                Assert.Equal(ex, observableResult.Messages.Where(m => m.Value.Kind == System.Reactive.NotificationKind.OnError).First().Value.Exception);
        }
    }
}