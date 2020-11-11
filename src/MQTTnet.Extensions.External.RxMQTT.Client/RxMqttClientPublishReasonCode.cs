namespace MQTTnet.Extensions.External.RxMQTT.Client
{
    /// <summary>
    /// Reasons for a rx mqtt publish result.
    /// </summary>
    public enum RxMqttClientPublishReasonCode
    {
        /// <summary>
        /// Message published.
        /// </summary>
        HasSucceeded,
        /// <summary>
        /// Message not published, because client was not connected.
        /// </summary>
        ClientNotConnected,
        /// <summary>
        /// Message not published, because publish has failed.
        /// </summary>
        HasFailed,
        /// <summary>
        /// Message not published, because message was skipped due message queue overflow.
        /// </summary>
        HasSkipped
    }
}