using System;
using GridDomain.EventSourcing;

namespace GridDomain.Tests.Unit.Sagas.SoftwareProgrammingDomain
{
    public class HomeAggregate : Aggregate
    {
        private HomeAggregate(Guid id) : base(id) {}

        public HomeAggregate(Guid id, Guid personId) : base(id)
        {
            Emit(new HomeCreated(id, personId));
        }

        public Guid PersonId { get; private set; }
        public int SleepTimes { get; private set; }

        private void Apply(HomeCreated e)
        {
            PersonId = e.PersonId;
        }

        private void Apply(Slept e)
        {
            SleepTimes++;
        }

        public void Sleep(Guid sofaId)
        {
            if (sofaId == Guid.Empty)
                throw new CantFindSofaException();

            Emit(new Slept(sofaId));
        }
    }
}