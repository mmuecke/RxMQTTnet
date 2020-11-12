using ConsoleTools;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.External.RxMQTT.Client;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Reactive.Linq;
using System.Threading;

namespace RxMqttClinetDemo
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            new ConsoleMenu()
               .Add("SubscribeExcample", () => SubscribeExcample())
               .Add("PublishExcample", () => PublishExcample())
               .Add("Close", ConsoleMenu.Close)
               .Configure(config =>
               {
                   config.Title = "RxMQTTClient Demo";
                   config.EnableWriteTitle = true;
                   config.WriteHeaderAction = () => Console.WriteLine("Pick an demo:");
               })
               .Show();
        }

        private static void PublishExcample()
        {
            // Setup and start a rx MQTT client.
            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId("Client1")
                    .WithTcpServer("127.0.0.1")
                    .Build())
                .Build();

            using var mqttClient = new MqttFactory().CreateRxMqttClient();
            _ = mqttClient.StartAsync(options);

            using var sub = Observable.Interval(TimeSpan.FromMilliseconds(1000))
                   .Select(i => new MqttApplicationMessageBuilder()
                       .WithTopic("MyTopic")
                       .WithPayload("Hello World rx: " + DateTime.Now.ToLongTimeString())
                       .WithExactlyOnceQoS()
                       .WithRetainFlag()
                       .Build())
                   .Publish(mqttClient)
                   .Subscribe(r => Console.WriteLine($"{r.ReasonCode} [{r.MqttApplicationMessage.Id}] :" +
                       $" {r.MqttApplicationMessage.ApplicationMessage.Payload.ToUTF8String()}"));

            WaitForExit("Publish a message every secound.");
        }

        private static void SubscribeExcample()
        {
            // Setup and start a rx MQTT client.
            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId("Client1")
                    .WithTcpServer("127.0.0.1")
                    .Build())
                .Build();

            using var mqttClient = new MqttFactory().CreateRxMqttClient();
            _ = mqttClient.StartAsync(options);

            var topic = "MyTopic/#";

            mqttClient.Connect(topic)
                .Select(message => new { message.ApplicationMessage.Topic, Payload = message.ApplicationMessage.Payload.ToUTF8String() })
                .Subscribe(message => Console.WriteLine($"@{message.Topic}: {message.Payload}"));

            WaitForExit($"Subscribed to {topic}.");
        }

        private static void WaitForExit(string message = null, bool clear = true)
        {
            if (clear)
                Console.Clear();
            if (message != null)
                Console.WriteLine(message);
            Console.WriteLine("Exit wiht 'ESC' or 'E'.");
            Console.WriteLine();

            while (Console.ReadKey(true).Key is ConsoleKey key && !(key == ConsoleKey.Escape || key == ConsoleKey.E))
                Thread.Sleep(1);
        }
    }
}