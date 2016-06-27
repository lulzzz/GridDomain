﻿using GridDomain.Node.Configuration;
using GridDomain.Node.Configuration.Akka;
using GridDomain.Node.Configuration.Persistence;
using GridDomain.Tests.Framework.Configuration;

namespace GridDomain.Tests.Framework
{
    public static class TestEnvironment
    {
        public static readonly IDbConfiguration Configuration
            = new AutoTestLocalDbConfiguration();

        public static readonly IAkkaDbConfiguration AkkaConfiguration
            = new AutoTestAkkaDbConfiguration();
    }
}