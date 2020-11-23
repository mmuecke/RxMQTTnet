using System;
using System.Reactive.Linq;
using Xunit;

namespace MQTTnet.Extensions.External.RxMQTT.Client.Test
{
    // the function is tested via the public extension methods
    public class MessagePayloadTransformTest
    {
        [Fact]
        public void CTOR_ArgumentNullExcepoin()
        {
            Assert.Throws<ArgumentNullException>(() => new MessageTransform<byte[]>(null, p => p, false));
        }

        [Fact]
        public void CTOR_ArgumentNullExcepoin_GetPayloadFunc()
        {
            Assert.Throws<ArgumentNullException>(() => new MessageTransform<byte[]>(Observable.Never<MqttApplicationMessage>(), null, false));
        }
    }
}