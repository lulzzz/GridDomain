using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;

namespace GridDomain.Common
{

    public class ProcessHistory
    {
        private readonly List<ProcessEntry> _steps;

        public ProcessHistory(IEnumerable<ProcessEntry> steps)
        {
            _steps = (steps ?? Enumerable.Empty<ProcessEntry>()).ToList();
        }

        public IReadOnlyCollection<ProcessEntry> Steps => _steps;

        public void Add(ProcessEntry entry)
        {
            _steps.Add(entry);
        }
    }
}