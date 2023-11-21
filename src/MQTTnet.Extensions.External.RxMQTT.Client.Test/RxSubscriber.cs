using Autofac.Extras.Moq;
using Microsoft.Reactive.Testing;
using Moq;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Packets;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
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
			mock.Mock<IMqttNetLogger>().Setup(x => x.IsEnabled).Returns(true);

			var rxMqttClinet = mock.Create<RxMqttClient>();
			var testScheduler = new TestScheduler();

			var result = testScheduler.Start(() => rxMqttClinet.Connect("Topic"), 0, 0, 1);

			Assert.Single(result.Messages);
			Assert.Single(result.Messages.Where(record => record.Value.Kind == NotificationKind.OnError));
			Assert.Equal(exceptin, result.Messages.Where(record => record.Value.Kind == NotificationKind.OnError).Single().Value.Exception);
			mock.Mock<IMqttNetLogger>().Verify(x => x.Publish(It.IsAny<MqttNetLogLevel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>(), exceptin), Times.Once);
			mock.Mock<IManagedMqttClient>().Verify(x => x.SubscribeAsync(It.IsAny<MqttTopicFilter[]>()), Times.Once);
		}

		[Fact]
		public void Disconnect_UnsubscribeAsync_Exception()
		{
			var exceptin = new Exception("Test");
			using var mock = AutoMock.GetLoose();
			mock.Mock<IManagedMqttClient>().Setup(x => x.SubscribeAsync(It.IsAny<MqttTopicFilter[]>())).Returns(Task.CompletedTask);
			mock.Mock<IManagedMqttClient>().Setup(x => x.UnsubscribeAsync(It.IsAny<string[]>())).Throws(exceptin);
			mock.Mock<IMqttNetLogger>().Setup(x => x.IsEnabled).Returns(true);

			var rxMqttClinet = mock.Create<RxMqttClient>();
			var testScheduler = new TestScheduler();

			testScheduler.Schedule(TimeSpan.FromTicks(2), () => rxMqttClinet.Connect("Topic"));

			var result = testScheduler.Start(() => rxMqttClinet.Connect("Topic"), 0, 0, 3);

			Assert.Empty(result.Messages);
			mock.Mock<IMqttNetLogger>().Verify(x => x.Publish(It.IsAny<MqttNetLogLevel>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>(), exceptin), Times.Once);
			mock.Mock<IManagedMqttClient>().Verify(x => x.SubscribeAsync(It.IsAny<MqttTopicFilter[]>()), Times.Once);
		}

		[Fact]
		public void Disconnect_UnsubscribeAsync_ObjectDisposedException_Leads_To_No_Error()
		{
			using var mock = AutoMock.GetLoose();
			mock.Mock<IManagedMqttClient>().Setup(x => x.SubscribeAsync(It.IsAny<MqttTopicFilter[]>())).Returns(Task.CompletedTask);
			mock.Mock<IManagedMqttClient>().Setup(x => x.UnsubscribeAsync(It.IsAny<string[]>())).Throws(new ObjectDisposedException(nameof(ManagedMqttClient)));
			mock.Mock<IMqttNetLogger>();
			var rxMqttClinet = mock.Create<RxMqttClient>();
			var testScheduler = new TestScheduler();

			testScheduler.Schedule(TimeSpan.FromTicks(2), () => rxMqttClinet.Connect("Topic"));

			var result = testScheduler.Start(() => rxMqttClinet.Connect("Topic"), 0, 0, 3);

			Assert.Empty(result.Messages);
			mock.Mock<IMqttNetLogger>().Verify(x => x.Publish(MqttNetLogLevel.Error, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<Exception>()), Times.Never);
		}

		[Fact]
		public void Publish_2Subscribe_2Recive_1Dispose_1Recive_Dispose()
		{
			using var mock = AutoMock.GetLoose();
			mock.Mock<IManagedMqttClient>();

			var rxMqttClinet = mock.Create<RxMqttClient>();

			var message1 = new MqttApplicationMessageBuilder()
				.WithTopic("T")
				.WithPayload("P1")
				.WithQualityOfServiceLevel(Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
				.Build();

			var message2 = new MqttApplicationMessageBuilder()
				.WithTopic("T")
				.WithPayload("P2")
				.WithQualityOfServiceLevel(Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
				.Build();
			var eventArgs1 = new MqttApplicationMessageReceivedEventArgs("1", message1, mock.Create<MqttPublishPacket>(), null);
			var eventArgs2 = new MqttApplicationMessageReceivedEventArgs("1", message2, mock.Create<MqttPublishPacket>(), null);

			var testScheduler = new TestScheduler();

			// act
			var firstCount = 0;
			var first = rxMqttClinet.Connect("T").Subscribe(_ => firstCount++);

			testScheduler.Schedule(TimeSpan.FromTicks(2), () => mock.Mock<IManagedMqttClient>().Raise(x => x.ApplicationMessageReceivedAsync += null, (object)eventArgs1));
			testScheduler.Schedule(TimeSpan.FromTicks(3), () => first.Dispose());
			testScheduler.Schedule(TimeSpan.FromTicks(4), () => mock.Mock<IManagedMqttClient>().Raise(x => x.ApplicationMessageReceivedAsync += null, (object)eventArgs2));

			var result = testScheduler.Start(() =>
			{
				IObservable<MqttApplicationMessageReceivedEventArgs> first = rxMqttClinet.Connect("T");
				return rxMqttClinet.Connect("T");
			}, 0, 0, 5);

			// test
			Assert.Equal(2, result.Messages.Count);
			Assert.Equal(NotificationKind.OnNext, result.Messages.First().Value.Kind);
			Assert.Equal(NotificationKind.OnNext, result.Messages.Last().Value.Kind);
			Assert.Equal(eventArgs1, result.Messages.First().Value.Value);
			Assert.Equal(eventArgs2, result.Messages.Last().Value.Value);
			Assert.Equal(1, firstCount);
		}

		[Fact]
		public void Publish_Subscribe_Once_And_2Recive_Dispose()
		{
			using var mock = AutoMock.GetLoose();
			mock.Mock<IManagedMqttClient>();

			var rxMqttClinet = mock.Create<RxMqttClient>();

			var message1 = new MqttApplicationMessageBuilder()
				.WithTopic("T")
				.WithPayload("P1")
				.WithQualityOfServiceLevel(Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
				.Build();

			var message2 = new MqttApplicationMessageBuilder()
				.WithTopic("T")
				.WithPayload("P2")
				.WithQualityOfServiceLevel(Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
				.Build();
			var eventArgs1 = new MqttApplicationMessageReceivedEventArgs("1", message1, mock.Create<MqttPublishPacket>(), null);
			var eventArgs2 = new MqttApplicationMessageReceivedEventArgs("1", message2, mock.Create<MqttPublishPacket>(), null);

			var testScheduler = new TestScheduler();

			// act
			testScheduler.Schedule(TimeSpan.FromTicks(2), () => mock.Mock<IManagedMqttClient>().Raise(x => x.ApplicationMessageReceivedAsync += null, (object)eventArgs1));
			testScheduler.Schedule(TimeSpan.FromTicks(3), () => mock.Mock<IManagedMqttClient>().Raise(x => x.ApplicationMessageReceivedAsync += null, (object)eventArgs2));
			var result = testScheduler.Start(() => rxMqttClinet.Connect("T"), 0, 0, 4);

			// test
			Assert.Equal(2, result.Messages.Count);
			Assert.Equal(NotificationKind.OnNext, result.Messages.First().Value.Kind);
			Assert.Equal(NotificationKind.OnNext, result.Messages.Last().Value.Kind);
			Assert.Equal(eventArgs1, result.Messages.First().Value.Value);
			Assert.Equal(eventArgs2, result.Messages.Last().Value.Value);
		}

		[Fact]
		public void Publish_Subscribe_Once_And_Dispose_Client()
		{
			using var mock = AutoMock.GetLoose();

			mock.Mock<IManagedMqttClient>();

			var rxMqttClinet = mock.Create<RxMqttClient>();

			var message = new MqttApplicationMessageBuilder()
				.WithTopic("T")
				.WithPayload("P")
				.WithQualityOfServiceLevel(Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
				.Build();
			var eventArgs = new MqttApplicationMessageReceivedEventArgs("1", message, mock.Create<MqttPublishPacket>(), null);
			var testScheduler = new TestScheduler();

			// act
			testScheduler.Schedule(TimeSpan.FromTicks(3), () => rxMqttClinet.Dispose());
			var result = testScheduler.Start(() => rxMqttClinet.Connect("T"), 0, 0, 4);

			// test
			Assert.Single(result.Messages);
			Assert.Equal(NotificationKind.OnCompleted, result.Messages.Single().Value.Kind);
		}

		[Fact]
		public void Publish_Subscribe_Once_And_NotReciveDueFilter_Dispose()
		{
			using var mock = AutoMock.GetLoose();
			mock.Mock<IManagedMqttClient>();

			var rxMqttClinet = mock.Create<RxMqttClient>();

			var message = new MqttApplicationMessageBuilder()
				.WithTopic("N")
				.WithPayload("P")
				.WithQualityOfServiceLevel(Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
				.Build();
			var eventArgs = new MqttApplicationMessageReceivedEventArgs("1", message, mock.Create<MqttPublishPacket>(), null);
			var testScheduler = new TestScheduler();

			// act
			testScheduler.Schedule(TimeSpan.FromTicks(2), () => mock.Mock<IManagedMqttClient>().Raise(x => x.ApplicationMessageReceivedAsync -= null, (object)eventArgs));
			var result = testScheduler.Start(() => rxMqttClinet.Connect("T"), 0, 0, 3);

			// test
			Assert.Empty(result.Messages);
		}

		[Fact]
		public void Publish_Subscribe_Once_And_Recive_Dispose()
		{
			using var mock = AutoMock.GetLoose();

			mock.Mock<IManagedMqttClient>();

			var rxMqttClinet = mock.Create<RxMqttClient>();

			var message = new MqttApplicationMessageBuilder()
				.WithTopic("T")
				.WithPayload("P")
				.WithQualityOfServiceLevel(Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
				.Build();
			var eventArgs = new MqttApplicationMessageReceivedEventArgs("1", message, mock.Create<MqttPublishPacket>(), null);
			var testScheduler = new TestScheduler();

			// act
			testScheduler.Schedule(TimeSpan.FromTicks(2), () => mock.Mock<IManagedMqttClient>().Raise(x => x.ApplicationMessageReceivedAsync -= null, (object)eventArgs));
			var result = testScheduler.Start(() => rxMqttClinet.Connect("T"), 0, 0, 3);

			// test
			Assert.Single(result.Messages);
			Assert.Equal(NotificationKind.OnNext, result.Messages.Single().Value.Kind);
			Assert.Equal(eventArgs, result.Messages.Single().Value.Value);
		}

		[Fact]
		public void Publish_Subscribe_WithTwoSubscriptions_ReciveOnce()
		{
			using var mock = AutoMock.GetLoose();
			mock.Mock<IManagedMqttClient>();

			var rxMqttClinet = mock.Create<RxMqttClient>();

			var message1 = new MqttApplicationMessageBuilder()
				.WithTopic("T/A")
				.WithPayload("P1")
				.WithQualityOfServiceLevel(Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
				.Build();

			var message2 = new MqttApplicationMessageBuilder()
				.WithTopic("T/A")
				.WithPayload("P2")
				.WithQualityOfServiceLevel(Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
				.Build();
			var eventArgs1 = new MqttApplicationMessageReceivedEventArgs("1", message1, mock.Create<MqttPublishPacket>(), null);
			var eventArgs2 = new MqttApplicationMessageReceivedEventArgs("1", message2, mock.Create<MqttPublishPacket>(), null);

			var testScheduler = new TestScheduler();

			// act
			testScheduler.Schedule(TimeSpan.FromTicks(2), () => mock.Mock<IManagedMqttClient>().Raise(x => x.ApplicationMessageReceivedAsync += null, (object)eventArgs1));
			testScheduler.Schedule(TimeSpan.FromTicks(3), () => mock.Mock<IManagedMqttClient>().Raise(x => x.ApplicationMessageReceivedAsync += null, (object)eventArgs2));

			// set up first subscription
			var result = testScheduler
				.Start(() => rxMqttClinet.Connect("T/A").Merge(rxMqttClinet.Connect("T/+")), 0, 0, 4);

			// test
			Assert.Equal(4, result.Messages.Count);
			Assert.Equal(NotificationKind.OnNext, result.Messages[0].Value.Kind);
			Assert.Equal(NotificationKind.OnNext, result.Messages[1].Value.Kind);
			Assert.Equal(NotificationKind.OnNext, result.Messages[2].Value.Kind);
            Assert.Equal(NotificationKind.OnNext, result.Messages[3].Value.Kind);
			Assert.Equal(eventArgs1, result.Messages[0].Value.Value);
			Assert.Equal(eventArgs1, result.Messages[1].Value.Value);
            Assert.Equal(eventArgs2, result.Messages[2].Value.Value);
			Assert.Equal(eventArgs2, result.Messages[3].Value.Value);
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
				.WithQualityOfServiceLevel(Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
				.Build();

			var mangedMessage = new ManagedMqttApplicationMessageBuilder()
				.WithApplicationMessage(message)
				.Build();

			// act
			await rxMqttClinet.PublishAsync(mangedMessage);

			// test
			mock.Mock<IManagedMqttClient>().Verify(x => x.EnqueueAsync(mangedMessage));
		}
	}
}