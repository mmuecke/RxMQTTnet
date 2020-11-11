using MQTTnet.Extensions.ManagedClient;
using System;

namespace MQTTnet.Extensions.External.RxMQTT.Client
{
    /// <summary>
    /// A result for publishing messages form a observable.
    /// </summary>
    public class RxMqttClientPublishResult
    {
        /// <summary>
        /// The reason code for the result.
        /// </summary>
        public RxMqttClientPublishReasonCode ReasonCode { get; set; }

        /// <summary>
        /// The processed message
        /// </summary>
        public ManagedMqttApplicationMessage MqttApplicationMessage { get; set; }

        /// <summary>
        /// Optional a exception during processing.
        /// </summary>
        public Exception Exception { get; set; }
    }
}