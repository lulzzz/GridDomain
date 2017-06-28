using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging;
using GridDomain.CQRS.Messaging.MessageRouting;
using GridDomain.EventSourcing;
using GridDomain.Tests.Unit.BalloonDomain.Events;
using GridDomain.Tests.Unit.BalloonDomain.ProjectionBuilders;

namespace GridDomain.Tests.Unit.BalloonDomain
{
   // public class BalloonRouteMap : IMessageRouteMap
   // {
   //     public async Task Register(IMessagesRouter router)
   //     {
   //         await router.RegisterAggregate(BalloonCommandHandler.Descriptor);
   //         await router.RegisterHandler<BalloonTitleChanged, BalloonTitleChangedNotificator>(m => m.SourceId);
   //         await router.RegisterHandler<BalloonTitleChanged, AggregateChangedProjectionBuilder>(m => m.SourceId);
   //         await router.RegisterHandler<BalloonCreated, BalloonCreatedNotificator>(m => m.SourceId);
   //         await router.RegisterHandler<BalloonCreated, AggregateCreatedProjectionBuilder_Alternative>(m => m.SourceId);
   //     }
   // }

   
}