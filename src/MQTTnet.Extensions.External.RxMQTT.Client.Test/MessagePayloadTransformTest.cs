using System;
using System.Reactive.Linq;
using Xunit;

namespace MQTTnet.Extensions.External.RxMQTT.Client.Test
{
    // the function is testet via the public extension methods
    public class MessagePayloadTransformTest
    {
        [Fact]
        public void CTOR_ArgumentNullExcepoin()
        {
            Assert.Throws<ArgumentNullException>(() => new MessagePayloadTransform<byte[]>(null, p => p, false));
        }

        [Fact]
        public void CTOR_ArgumentNullExcepoin_GetPayloadFunc()
        {
            Assert.Throws<ArgumentNullException>(() => new MessagePayloadTransform<byte[]>(Observable.Never<MqttApplicationMessage>(), null, false));
        }
    }
}