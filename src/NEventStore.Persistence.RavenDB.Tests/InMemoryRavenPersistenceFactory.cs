using NEventStore.Serialization;
using Raven.Client;
using Raven.Client.Listeners;
using Raven.Client.Embedded;
using System;
namespace NEventStore.Persistence.RavenDB.Tests
{

  public class InMemoryRavenPersistenceFactory : RavenPersistenceFactory
  {
    public InMemoryRavenPersistenceFactory(string connectionName, IDocumentSerializer serializer, RavenPersistenceOptions options)
      : base(connectionName, serializer, options)
    {

    }

    public override IPersistStreams Build()
    {
      var embeddedStore = new EmbeddableDocumentStore();
      embeddedStore.Configuration.RunInMemory = true;
      //embeddedStore.Configuration.RunInUnreliableYetFastModeThatIsNotSuitableForProduction = true;
      embeddedStore.RegisterListener(new CheckpointNumberIncrementListener(embeddedStore));
      embeddedStore.Initialize();
      return new RavenPersistenceEngine(embeddedStore, serializer, options);
    }

    #region Old
    //public InMemoryRavenPersistenceFactory(Raven.Database.Config.RavenConfiguration config)
    //  : base(config)
    //{ }

    //protected override IDocumentStore GetStore()
    //{
    //  return new EmbeddableDocumentStore { RunInMemory = true }.Initialize();
    //}
    #endregion
  }
}