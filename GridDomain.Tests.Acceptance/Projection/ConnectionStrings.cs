using GridDomain.Tests.Common;

namespace GridDomain.Tests.Acceptance.Projection {
    public static class ConnectionStrings
    {
        static ConnectionStrings()
        {
            AutoTestDb =
                         new AutoTestLocalDbConfiguration().ReadModelConnectionString;
        }

        public static string AutoTestDb { get; }
    }
}