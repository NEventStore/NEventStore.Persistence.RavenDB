// ReSharper disable once CheckNamespace
namespace NEventStore.Persistence.AcceptanceTests
{
  using NEventStore.Persistence.RavenDB.Tests;

  public partial class PersistenceEngineFixture
  {
    public PersistenceEngineFixture()
    {
      //_createPersistence = _ => new InMemoryRavenPersistenceFactory(TestRavenConfig.GetDefaultConfig()).Build();
      _createPersistence = _ => new InMemoryRavenPersistenceFactory(
        TestRavenConfig.ConnectionName,
        TestRavenConfig.Serializer,
        new NEventStore.Persistence.RavenDB.RavenPersistenceOptions(TestRavenConfig.PageSize, TestRavenConfig.ConsistentQueries, TestRavenConfig.ScopeOption)
      ).Build();
    }
  }
}