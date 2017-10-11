﻿using System;
using System.Threading.Tasks;
using GridDomain.ProcessManagers;
using GridDomain.Tests.Common;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain;
using GridDomain.Tests.Unit.ProcessManagers.SoftwareProgrammingDomain.Events;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Unit.ProcessManagers.Transitions
{
    public class Given_processActor_When_valid_Transitions
    { 
        public Given_processActor_When_valid_Transitions(ITestOutputHelper output)
        {
            _log = new XUnitAutoTestLoggerConfiguration(output).CreateLogger();
        }

        private readonly ILogger _log;

        [Fact]
        public async Task Commands_are_produced()
        {
            var given = new Given_Automatonymous_Process(m => m.Coding);

            var subscriptionExpiredEvent = new GotTiredEvent(Guid.NewGuid());
            var newState = await given.Process.Transit(given.State, subscriptionExpiredEvent);

            Assert.NotEmpty(newState.ProducedCommands);
        }

        [Fact]
        public async Task Process_state_is_changed_after_transition_by_event_data()
        {
            var given = new Given_Automatonymous_Process(m => m.Coding);

            var subscriptionExpiredEvent = new GotTiredEvent(Guid.NewGuid());
            var newState = await given.Process.Transit(given.State, subscriptionExpiredEvent);

            Assert.Equal(subscriptionExpiredEvent.SourceId, newState.State.PersonId);
        }

        [Fact]
        public async Task State_in_transition_result_is_changed()
        {
            var given = new Given_Automatonymous_Process(m => m.MakingCoffee);
            var newState = await given.Process.Transit(given.State, new CoffeMadeEvent(Guid.NewGuid(), Guid.NewGuid()));
            Assert.Equal(nameof(SoftwareProgrammingProcess.Coding), newState.State.CurrentStateName);
        }

        [Fact]
        public async Task Process_state_not_changed()
        {
            var given = new Given_Automatonymous_Process(m => m.MakingCoffee);

            var stateBefore = given.State.CurrentStateName;

            await given.Process.Transit(given.State, new CoffeMadeEvent(Guid.NewGuid(), Guid.NewGuid()));

            var stateAfter = given.State.CurrentStateName;

            Assert.Equal(stateBefore, stateAfter);
        }

        [Fact]
        public async Task State_is_changed_on_using_non_generic_transit_method()
        {
            var given = new Given_Automatonymous_Process(m => m.MakingCoffee);
            object msg = new CoffeMadeEvent(Guid.NewGuid(), Guid.NewGuid());
            var newState =  await given.Process.Transit(given.State, msg);
            Assert.Equal(nameof(SoftwareProgrammingProcess.Coding), newState.State.CurrentStateName);
        }

        [Fact]
        public async Task When_apply_known_but_not_mapped_event_in_state()
        {
            var given = new Given_Automatonymous_Process(m => m.Sleeping);
            var gotTiredEvent = new GotTiredEvent(Guid.NewGuid());
            await given.Process.Transit(given.State,gotTiredEvent).ShouldThrow<ProcessTransitionException>();
        }
    }
}