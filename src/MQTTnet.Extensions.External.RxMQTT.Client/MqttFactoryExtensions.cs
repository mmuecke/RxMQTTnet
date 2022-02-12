using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using System;

namespace MQTTnet.Extensions.External.RxMQTT.Client
{
    /// <summary>
    /// Extension to <see cref="MqttFactory"/> to crate a <see cref="IRxMqttClient"/>.
    /// </summary>
    public static class MqttFactoryExtensions
    {
        /// <summary>
        /// Crate a <see cref="IRxMqttClient"/> from the factory.
        /// </summary>
        /// <param name="factory">The factory to use.</param>
        /// <returns>The <see cref="IRxMqttClient"/>.</returns>
        public static IRxMqttClient CreateRxMqttClient(this MqttFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            return new RxMqttClient(factory.CreateManagedMqttClient(), factory.DefaultLogger);
        }

        /// <summary>
        /// Crate a <see cref="IRxMqttClient"/> from the factory.
        /// </summary>
        /// <param name="factory">The factory to use.</param>
        /// <param name="logger">The mqtt net logger to use.</param>
        /// <returns>The <see cref="IRxMqttClient"/>.</returns>
        public static IRxMqttClient CreateRxMqttClient(this MqttFactory factory, IMqttNetLogger logger)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            return new RxMqttClient(factory.CreateManagedMqttClient(logger), logger);
        }
    }
}