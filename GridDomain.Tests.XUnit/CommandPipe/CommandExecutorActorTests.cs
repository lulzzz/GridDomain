﻿using System;
using Akka.Actor;
using Akka.TestKit.Xunit2;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.Node.Actors.CommandPipe;
using GridDomain.Tests.XUnit.SampleDomain.Commands;
using Xunit;

namespace GridDomain.Tests.XUnit.CommandPipe
{
    public class CommandExecutorActorTests : TestKit
    {
        private class CreateCommand : CreateSampleAggregateCommand
        {
            public CreateCommand(int parameter, Guid aggregateId) : base(parameter, aggregateId) {}
        }

        [Fact]
        public void CommandExecutor_does_not_support_command_inheritance()
        {
            var catalog = new TypeCatalog<Processor, ICommand>();
            catalog.Add<CreateSampleAggregateCommand>(new Processor(TestActor));

            var actor = Sys.ActorOf(Props.Create(() => new AggregatesPipeActor(catalog)));

            var msg = new MessageMetadataEnvelop<CreateCommand>(new CreateCommand(1, Guid.NewGuid()), MessageMetadata.Empty);

            actor.Tell(msg);

            ExpectNoMsg();
        }

        [Fact]
        public void CommandExecutor_routes_command_by_its_type()
        {
            var catalog = new TypeCatalog<Processor, ICommand>();
            catalog.Add<CreateSampleAggregateCommand>(new Processor(TestActor));

            var actor = Sys.ActorOf(Props.Create(() => new AggregatesPipeActor(catalog)));

            var msg = new MessageMetadataEnvelop<ICommand>(new CreateSampleAggregateCommand(1, Guid.NewGuid()),
                                                           MessageMetadata.Empty);

            actor.Tell(msg);

            ExpectMsg<MessageMetadataEnvelop<ICommand>>();
        }
    }
}