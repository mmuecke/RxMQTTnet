﻿using MQTTnet.Extensions.ManagedClient;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace MQTTnet.Extensions.External.RxMQTT.Client
{
    /// <summary>
    /// Extensions to publish messages from a observer.
    /// </summary>
    public static class RxMqttPublishExtensions
    {
        /// <summary>
        /// Publish the stream of <see cref="ManagedMqttApplicationMessage"/>s to the server and give a response of the publish result.
        /// </summary>
        /// <param name="rxMqttClinet">The client to publish the messages with.</param>
        /// <param name="observable">The source observable.</param>
        /// <returns>A observer for the publish results.</returns>
        public static IObservable<RxMqttClientPublishResult> Publish(this IRxMqttClient rxMqttClinet,
        IObservable<ManagedMqttApplicationMessage> observable)
        {
            if (rxMqttClinet is null) throw new ArgumentNullException(nameof(rxMqttClinet));
            if (observable is null) throw new ArgumentNullException(nameof(observable));

            return observable.PublishOn(rxMqttClinet);
        }

        /// <summary>
        /// Publish the stream of <see cref="MqttApplicationMessage"/>s to the server and give a response of the publish result.
        /// </summary>
        /// <param name="rxMqttClinet">The client to publish the messages with.</param>
        /// <param name="observable">The source observable.</param>
        /// <returns>A observer for the publish results.</returns>
        public static IObservable<RxMqttClientPublishResult> Publish(this IRxMqttClient rxMqttClinet,
        IObservable<MqttApplicationMessage> observable)
        {
            if (rxMqttClinet is null) throw new ArgumentNullException(nameof(rxMqttClinet));
            if (observable is null) throw new ArgumentNullException(nameof(observable));

            return observable.PublishOn(rxMqttClinet);
        }

        /// <summary>
        /// Publish the stream of <see cref="MqttApplicationMessage"/>s to the server and give a response of the publish result.
        /// </summary>
        /// <param name="observable">The source observable.</param>
        /// <param name="rxMqttClinet">The client to publish the messages with.</param>
        /// <returns>A observer for the publish results.</returns>
        public static IObservable<RxMqttClientPublishResult> PublishOn(this IObservable<MqttApplicationMessage> observable,
        IRxMqttClient rxMqttClinet)
        {
            if (observable is null) throw new ArgumentNullException(nameof(observable));
            if (rxMqttClinet is null) throw new ArgumentNullException(nameof(rxMqttClinet));

            return observable
                .Select(message => new ManagedMqttApplicationMessageBuilder().WithApplicationMessage(message).Build())
                .PublishOn(rxMqttClinet);
        }

        /// <summary>
        /// Publish the stream of <see cref="ManagedMqttApplicationMessage"/>s to the server and give a response of the publish result.
        /// </summary>
        /// <param name="observable">The source observable.</param>
        /// <param name="rxMqttClinet">The client to publish the messages with.</param>
        /// <returns>A observer for the publish results.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Is forwarded to the observer")]
        public static IObservable<RxMqttClientPublishResult> PublishOn(this IObservable<ManagedMqttApplicationMessage> observable, IRxMqttClient rxMqttClinet)
        {
            if (observable is null) throw new ArgumentNullException(nameof(observable));
            if (rxMqttClinet is null) throw new ArgumentNullException(nameof(rxMqttClinet));

            return observable
                .Select(managedMqttApplicationMessage =>
                {
                    return Observable.Create<RxMqttClientPublishResult>(async observer =>
                    {
                        if (!rxMqttClinet.IsConnected)
                        {
                            observer.OnNext(new RxMqttClientPublishResult { ReasonCode = RxMqttClientPublishReasonCode.ClientNotConnected, MqttApplicationMessage = managedMqttApplicationMessage });
                            observer.OnCompleted();
                            return Disposable.Empty;
                        }
                        else
                        {
                            var subscription = rxMqttClinet.ApplicationMessageProcessedEvent
                                .Where(@event => @event.ApplicationMessage.Id == managedMqttApplicationMessage.Id)
                                .Select(@event => @event.HasSucceeded
                                    ? new RxMqttClientPublishResult { ReasonCode = RxMqttClientPublishReasonCode.HasSucceeded, MqttApplicationMessage = @event.ApplicationMessage }
                                    : new RxMqttClientPublishResult { ReasonCode = RxMqttClientPublishReasonCode.HasFailed, MqttApplicationMessage = @event.ApplicationMessage, Exception = @event.Exception })
                                .Merge(rxMqttClinet.ApplicationMessageSkippedEvent
                                    .Where(@event => @event.ApplicationMessage.Id == managedMqttApplicationMessage.Id)
                                    .Select(@event => new RxMqttClientPublishResult { ReasonCode = RxMqttClientPublishReasonCode.HasSkipped, MqttApplicationMessage = @event.ApplicationMessage }))
                                .Take(1)
                                .SubscribeSafe(observer);

                            try
                            {
                                await rxMqttClinet.PublishAsync(managedMqttApplicationMessage).ConfigureAwait(false);
                            }
                            catch (Exception exception)
                            {
                                observer.OnNext(new RxMqttClientPublishResult { ReasonCode = RxMqttClientPublishReasonCode.HasFailed, MqttApplicationMessage = managedMqttApplicationMessage, Exception = exception });
                                observer.OnCompleted();
                            }

                            return subscription;
                        }
                    });
                })
                .Merge();
        }
    }
}