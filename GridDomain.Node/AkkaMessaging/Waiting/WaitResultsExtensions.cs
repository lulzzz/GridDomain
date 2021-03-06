using System;
using System.Linq;
using GridDomain.Common;
using GridDomain.CQRS;

namespace GridDomain.Node.AkkaMessaging.Waiting
{
    public static class WaitResultsExtensions
    {
        public static TMsg Message<TMsg>(this IWaitResult res, Predicate<TMsg> selector = null) where TMsg : class
        {
            var sel = selector ?? (m => true);
            var msg = res.All.OfType<TMsg>().FirstOrDefault(t => sel(t));
            return msg ?? MessageWithMetadata(res, selector)?.Message;
        }

        public static IMessageMetadataEnvelop<TMsg> MessageWithMetadata<TMsg>(this IWaitResult res,
                                                                              Predicate<TMsg> selector = null)
        {
            var sel = selector ?? (m => true);
            var loosyTypedEnvelop = res.All.OfType<IMessageMetadataEnvelop>().First(t => t.Message is TMsg && sel((TMsg)t.Message));
            return new MessageMetadataEnvelop<TMsg>((TMsg)loosyTypedEnvelop.Message, loosyTypedEnvelop.Metadata);
        }
    }
}