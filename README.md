![Build](https://github.com/mmuecke/RxMQTTnet/workflows/Build/badge.svg) [![codecov](https://codecov.io/gh/mmuecke/RxMQTTnet/branch/main/graph/badge.svg?token=8KtPaZ3VZB)](https://codecov.io/gh/mmuecke/RxMQTTnet) 
[![NuGet Stats](https://img.shields.io/nuget/v/MQTTnet.Extensions.External.RxMQTT.Client.svg)](https://www.nuget.org/packages/MQTTnet.Extensions.External.RxMQTT.Client)
[![Downloads](https://img.shields.io/nuget/dt/MQTTnet.Extensions.External.RxMQTT.Client.svg)](https://www.nuget.org/packages/MQTTnet.Extensions.External.RxMQTT.Client)

# RxMQTTnet
An extension to the [MQTTnet](https://github.com/chkr1011/MQTTnet) project, to transform the subscriptions into observables and to publish from an observable stream.

# Create a client
## Use the factory
Use the `MQTTnet.MqttFactory` with the `MQTTnet.Extensions.External.RxMQTT.Client.MqttFactoryExtensions`.

```csharp
var client = new MqttFactory().CreateRxMqttClient();
```

## Create the options
Use the [managed client options](https://github.com/chkr1011/MQTTnet/wiki/ManagedClient#preparation)

## Start the client

```csharp
await client.StartAsync(options).ConfigureAwait(false);
```

# Subscribe
Get an `IObservable<MqttApplicationMessageReceivedEventArgs>` by connecting to the rx client and use extensions to process the message:

```csharp
var subscription = rxMqttClinet
    .Connect("RxClientTest/#")
    .GetPayload()
    .Subscribe(Console.WriteLine);
```

End the subscription by disposing the subscription.

```csharp
subscription.Dispose();
```

# Publish
## From observable

Create an observable sequence of `MqttApplicationMessage`s and publish these via the rx client.

```csharp
Observable.Interval(TimeSpan.FromMilliseconds(1000))
    .Select(i => new MqttApplicationMessageBuilder()
        .WithTopic("RxClientTest")
        .WithPayload("Time: " + DateTime.Now.ToLongTimeString())
        .WithExactlyOnceQoS()
        .WithRetainFlag()
        .Build())
    .PublishOn(mqttClient)
    .Subscribe();
```

## Single message
Use the [mqtt client publish method](https://github.com/chkr1011/MQTTnet/wiki/Client#publishing-messages).
