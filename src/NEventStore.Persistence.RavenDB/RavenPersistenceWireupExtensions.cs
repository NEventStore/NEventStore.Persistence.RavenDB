using NEventStore.Persistence.RavenDB;
// ReSharper disable once CheckNamespace
using NEventStore.Serialization;
namespace NEventStore
{
  public static class RavenPersistenceWireupExtensions
  {
    #region OldVersion
    //public static RavenPersistenceWireup UsingRavenPersistence(this Wireup wireup, string connectionName)
    //{
    //    return new RavenPersistenceWireup(wireup, connectionName);
    //}

    //public static RavenPersistenceWireup UsingRavenPersistence(this Wireup wireup)
    //{
    //    return new RavenPersistenceWireup(wireup);
    //}
    #endregion

    public static RavenPersistenceWireup UsingRavenPersistence(this Wireup wireup, string connectionName, IDocumentSerializer serializer)
    {
      return new RavenPersistenceWireup(wireup, connectionName, serializer);
    }
    
    public static RavenPersistenceWireup UsingRavenPersistence(this Wireup wireup, string connectionName, IDocumentSerializer serializer, RavenPersistenceOptions options)
    {
      return new RavenPersistenceWireup(wireup, connectionName, serializer, options);
    }
  }

}