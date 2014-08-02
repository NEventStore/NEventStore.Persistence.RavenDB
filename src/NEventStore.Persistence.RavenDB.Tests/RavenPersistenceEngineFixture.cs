namespace NEventStore.Persistence.RavenDB.Tests
{
    using System;

    public class RavenPersistenceEngineFixture : IDisposable
    {
        public RavenPersistenceEngineFixture()
        {
            //Persistence = (RavenPersistenceEngine)new InMemoryRavenPersistenceFactory(TestRavenConfig.GetDefaultConfig()).Build();
            Persistence = (RavenPersistenceEngine) new InMemoryRavenPersistenceFactory(
                TestRavenConfig.ConnectionName,
                TestRavenConfig.Serializer,
                new RavenPersistenceOptions(TestRavenConfig.PageSize, TestRavenConfig.ConsistentQueries, TestRavenConfig.ScopeOption)
                ).Build();
            Persistence.Initialize();
        }

        public RavenPersistenceEngine Persistence { get; private set; }

        public void Dispose()
        {
            Persistence.Dispose();
        }
    }
}