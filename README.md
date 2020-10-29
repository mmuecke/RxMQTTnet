![Build](https://github.com/mmuecke/RxMQTTnet/workflows/Build/badge.svg) [![Code Coverage](https://codecov.io/gh/mmuecke/RxMQTTnet/branch/master/graph/badge.svg)](https://codecov.io/gh/mmuecke/RxMQTTnet)
[![NuGet Stats](https://img.shields.io/nuget/v/MQTTnet.Extensions.External.RxMQTT.Client.svg)](https://www.nuget.org/packages/MQTTnet.Extensions.External.RxMQTT.Client) ![Downloads](https://img.shields.io/nuget/dt/MQTTnet.Extensions.External.RxMQTT.Client.svg)
<br />
<br />
<a href="https://github.com/reactiveui/DynamicData">
        <img width="170" height="170" src="https://github.com/reactiveui/styleguide/blob/master/logo_dynamic_data/logo.svg"/>
</a>

# RxMQTTnet
A extension to the [MQTTnet](https://github.com/chkr1011/MQTTnet) project, to transform the subscriptions into observables.

# Crate a client
## Preparation
Use the [managed clinent options](https://github.com/chkr1011/MQTTnet/wiki/ManagedClient#preparation)
## Factory
Use the `MQTTnet.MqttFactory` wiht the `MQTTnet.Extensions.External.RxMQTT.Client.MqttFactoryExtensions`
```csharp
var client = new MqttFactory().CreateRxMqttClient();
```

# Subscribe
Get a `IObservable<MqttApplicationMessageReceivedEventArgs>` by connecting to the rx client and use extensions to process the message:
```csharp
var unSubscribe = rxMqttClinet.Connect("SensorData/#").GetPayload().Subscribe(Console.WriteLine);
```
To end the subscribtion dispose the subscribtion.
```csharp
unSubscribe.Dispose();
```

# Publisch
Use the [mqtt client publish method](https://github.com/chkr1011/MQTTnet/wiki/Client#publishing-messages).
