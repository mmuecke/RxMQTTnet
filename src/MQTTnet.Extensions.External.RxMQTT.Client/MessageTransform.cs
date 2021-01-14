using System;
using System.Reactive.Linq;

namespace MQTTnet.Extensions.External.RxMQTT.Client
{
    /// <summary>
    /// A helper to transform the message to a new type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The transformed type.</typeparam>
    public class MessageTransform<T>
    {
        private readonly IObservable<MqttApplicationMessage> source;
        private readonly Func<byte[], T> getPayloadFunc;
        private readonly bool skipOnError;

        /// <summary>
        /// Crate a helper to transform the message to a new type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="source">The source message observable.</param>
        /// <param name="transformFunc">The transform function.</param>
        /// <param name="skipOnError">A flag to indicate to skip transform errors.</param>
        public MessageTransform(IObservable<MqttApplicationMessage> source, Func<byte[], T> transformFunc, bool skipOnError)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            getPayloadFunc = transformFunc ?? throw new ArgumentNullException(nameof(transformFunc));
            this.skipOnError = skipOnError;
        }

        /// <summary>
        /// Run the transform observer.
        /// </summary>
        /// <returns>The transformed observer.</returns>
        public IObservable<T> Run()
        {
            return Observable.Create<T>(observer =>
            {
                return source.Subscribe(message =>
                    {
                        try
                        {
                            observer.OnNext(getPayloadFunc(message.Payload));
                        }
                        catch (Exception exception)
                        {
                            if (!skipOnError)
                                observer.OnError(exception);
                        }
                    },
                    exception => observer.OnError(exception),
                    () => observer.OnCompleted());
            });
        }
    }
}