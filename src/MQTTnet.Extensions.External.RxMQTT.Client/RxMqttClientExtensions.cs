﻿using MQTTnet.Client;
using MQTTnet.Protocol;
using System;
using System.Reactive.Linq;
using System.Text;

namespace MQTTnet.Extensions.External.RxMQTT.Client
{
    /// <summary>
    /// Extensions for rx mqtt client.
    /// </summary>
    public static class RxMqttClientExtensions
    {
        /// <summary>
        /// Filter the stream by a <see cref="MqttQualityOfServiceLevel"/>.
        /// </summary>
        /// <param name="source">The source observable.</param>
        /// <param name="mqttQualityOfServiceLevel">The level to filter for.</param>
        /// <returns>The filtered source.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IObservable<MqttApplicationMessage> FilterQoS(
            this IObservable<MqttApplicationMessage> source, MqttQualityOfServiceLevel mqttQualityOfServiceLevel)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return source.Where(message => message.QualityOfServiceLevel == mqttQualityOfServiceLevel);
        }

        /// <summary>
        /// Filter the stream by a <see cref="MqttQualityOfServiceLevel"/>.
        /// </summary>
        /// <param name="source">The source observable.</param>
        /// <param name="mqttQualityOfServiceLevel">The level to filter for.</param>
        /// <returns>The filtered source.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IObservable<MqttApplicationMessageReceivedEventArgs> FilterQoS(
            this IObservable<MqttApplicationMessageReceivedEventArgs> source, MqttQualityOfServiceLevel mqttQualityOfServiceLevel)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return source.Where(@event => @event.ApplicationMessage.QualityOfServiceLevel == mqttQualityOfServiceLevel);
        }

        /// <summary>
        /// Filter the stream by a topic.
        /// </summary>
        /// <param name="source">The source observable.</param>
        /// <param name="filter">The level to filter for.</param>
        /// <returns>The filtered source.</returns>
        /// <remarks>Wildcards '#' and '+' are allowed.</remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static IObservable<MqttApplicationMessageReceivedEventArgs> FilterTopic(
            this IObservable<MqttApplicationMessageReceivedEventArgs> source, string filter)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(filter)) throw new ArgumentException($"'{nameof(filter)}' cannot be null or whitespace", nameof(filter));

            return Observable.Create<MqttApplicationMessageReceivedEventArgs>(observer =>
            {
                return source
                    .Where(@event => 
                    MqttTopicFilterComparer.Compare(@event.ApplicationMessage.Topic, filter) == MqttTopicFilterCompareResult.IsMatch)
                    .SubscribeSafe(observer);
            });
        }

        /// <summary>
        /// Filter the stream by a topic.
        /// </summary>
        /// <param name="source">The source observable.</param>
        /// <param name="filter">The level to filter for.</param>
        /// <returns>The filtered source.</returns>
        /// <remarks>Wildcards '#' and '+' are allowed.</remarks>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static IObservable<MqttApplicationMessage> FilterTopic(
            this IObservable<MqttApplicationMessage> source, string filter)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(filter)) throw new ArgumentException($"'{nameof(filter)}' cannot be null or whitespace", nameof(filter));

            return Observable.Create<MqttApplicationMessage>(observer =>
            {
                return source
                    .Where(message =>
                        MqttTopicFilterComparer.Compare(message.Topic, filter) == MqttTopicFilterCompareResult.IsMatch)
                    .SubscribeSafe(observer);
            });
        }

        /// <summary>
        /// Select the message from the event arguments.
        /// </summary>
        /// <param name="source">The source observable.</param>
        /// <returns>The selected messages observable.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IObservable<MqttApplicationMessage> SelectMessage(this IObservable<MqttApplicationMessageReceivedEventArgs> source)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return source.Select(@event => @event.ApplicationMessage);
        }

        /// <summary>
        /// Select the payload as <see cref="string"/> from the event arguments.
        /// </summary>
        /// <param name="source">The source observable.</param>
        /// <param name="skipOnError">Messages that can not be transformed are skipped.</param>
        /// <param name="defaultOnNull">The default string when payload is null.</param>
        /// <returns>The selected payload observable.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IObservable<string> SelectPayload(this IObservable<MqttApplicationMessageReceivedEventArgs> source, bool skipOnError = true, string defaultOnNull = "")
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return source.SelectMessage().SelectPayload(skipOnError, defaultOnNull);
        }

        /// <summary>
        /// Select the payload as <typeparamref name="T"/> from the event arguments.
        /// </summary>
        /// <param name="source">The source observable.</param>
        /// <param name="getPayloadFunc">The function to geht the payload form the <see cref="byte"/> array.</param>
        /// <param name="skipOnError">Messages that can not be transformed are skipped.</param>
        /// <returns>The selected payload observable.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IObservable<T> SelectPayload<T>(this IObservable<MqttApplicationMessageReceivedEventArgs> source, Func<byte[], T> getPayloadFunc, bool skipOnError = true)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return source.SelectMessage().SelectPayload(getPayloadFunc, skipOnError);
        }

        /// <summary>
        /// Select the payload as <see cref="string"/> from the message.
        /// </summary>
        /// <param name="source">The source observable.</param>
        /// <param name="skipOnError">Messages that can not be transformed are skipped.</param>
        /// <param name="defaultOnNull">The default string when payload is null.</param>
        /// <returns>The selected payload observable.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IObservable<string> SelectPayload(this IObservable<MqttApplicationMessage> source, bool skipOnError = true, string defaultOnNull = "")
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            return source.SelectPayload(payload => payload.ToUTF8String(defaultOnNull), skipOnError);
        }

        /// <summary>
        /// Select the payload as <typeparamref name="T"/> from the message.
        /// </summary>
        /// <param name="source">The source observable.</param>
        /// <param name="getPayloadFunc">The function to geht the payload form the <see cref="byte"/> array.</param>
        /// <param name="skipOnError">Messages that can not be transformed are skipped.</param>
        /// <returns>The selected payload observable.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IObservable<T> SelectPayload<T>(this IObservable<MqttApplicationMessage> source, Func<byte[], T> getPayloadFunc, bool skipOnError = true)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (getPayloadFunc is null) throw new ArgumentNullException(nameof(getPayloadFunc));

            return new MessageTransform<T>(source, getPayloadFunc, skipOnError).Run();
        }

        /// <summary>
        /// Get a UTF8 string form a byte array.
        /// </summary>
        /// <param name="byteArray">The byte array.</param>
        /// <param name="defaultOnNull">The default string if the array is empty.</param>
        /// <returns></returns>
        public static string ToUTF8String(this byte[] byteArray, string defaultOnNull = "")
        {
            return byteArray is null ? defaultOnNull : Encoding.UTF8.GetString(byteArray);
        }
    }
}