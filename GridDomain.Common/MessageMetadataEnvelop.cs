using System;
using System.Linq;
using System.Reflection;

namespace GridDomain.Common
{
    public class MessageMetadataEnvelop : IMessageMetadataEnvelop
    {
        public MessageMetadataEnvelop(object message, IMessageMetadata metadata = null)
        {
            Message = message;
            Metadata = metadata ?? MessageMetadata.Empty;
        }

        public object Message { get; }
        public IMessageMetadata Metadata { get; }

        public static MessageMetadataEnvelop New<T>(T msg, IMessageMetadata metadata = null)
        {
            return new MessageMetadataEnvelop<T>(msg, metadata);
        }
    }

    public class MessageMetadataEnvelop<T> : MessageMetadataEnvelop,
                                             IMessageMetadataEnvelop<T>
    {
        public MessageMetadataEnvelop(T message, IMessageMetadata metadata) : base(message, metadata) {}

        public new T Message => (T) base.Message;

     
    }
}