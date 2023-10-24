namespace MQTTnet.Extensions.External.RxMQTT.Client.Test
{
	using Autofac.Extras.Moq;
	using Microsoft.Reactive.Testing;
	using MQTTnet.Client;
	using MQTTnet.Extensions.ManagedClient;
	using MQTTnet.Packets;
	using System;
	using System.Linq;
	using System.Reactive.Concurrency;
	using System.Reactive.Linq;
	using Xunit;

	public class MultipleSimilarTopics
	{
		[Fact]
		public void ConnectMultipleSimilarTopics()
		{
			string connectTopic1 = "Test/MultiSimiliar/#";
			string connectTopic2 = "Test/MultiSimiliar/+/subtopic/next";

			string sendTopic = "Test/MultiSimiliar/bla/subtopic/next";

			using var mock = AutoMock.GetLoose();
			mock.Mock<IManagedMqttClient>();

			var rxMqttClient = mock.Create<RxMqttClient>();

			var message1 = new MqttApplicationMessageBuilder()
				.WithTopic(sendTopic)
				.WithPayload("Hello World!")
				.WithQualityOfServiceLevel(Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
				.Build();
			var eventArgs1 = new MqttApplicationMessageReceivedEventArgs("1", message1, mock.Create<MqttPublishPacket>(), null);

			var testScheduler = new TestScheduler();

			testScheduler.Schedule(TimeSpan.FromTicks(2), () => mock.Mock<IManagedMqttClient>().Raise(x => x.ApplicationMessageReceivedAsync += null, (object)eventArgs1));

			var result = testScheduler.Start(
				() => rxMqttClient.Connect(connectTopic1)
					.Select(message => (message, topic: 1))
					.Merge(rxMqttClient.Connect(connectTopic2)
						.Select(message => (message, topic: 2))),
				0, 0, 10);

			Assert.Single(result.Messages.Where(record => record.Value.Value.topic == 1));
			Assert.Single(result.Messages.Where(record => record.Value.Value.topic == 2));
		}
	}
}
