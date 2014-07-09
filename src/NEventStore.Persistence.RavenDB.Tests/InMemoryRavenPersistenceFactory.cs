using NEventStore.Serialization;
using System;
namespace NEventStore.Persistence.RavenDB.Tests
{

  public class InMemoryRavenPersistenceFactory : RavenPersistenceFactory
  {
    public InMemoryRavenPersistenceFactory(string connectionName, IDocumentSerializer serializer, RavenPersistenceOptions options)
      : base(connectionName, serializer, options)
    {

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