namespace NEventStore
{
  using NEventStore.Logging;
  using NEventStore.Persistence.RavenDB;
  using NEventStore.Serialization;

  public class RavenPersistenceWireup : PersistenceWireup
  {
    private static readonly ILog Logger = LogFactory.BuildLogger(typeof(RavenPersistenceWireup));

    public RavenPersistenceWireup(Wireup wireup, string connectionName)
      : this(wireup, connectionName, new RavenPersistenceOptions())
    { }

    public RavenPersistenceWireup(Wireup wireup, string connectionName, RavenPersistenceOptions persistenceOptions)
      : base(wireup)
    {
      Logger.Debug("Configuring Raven persistence engine.");
      Container.Register(c => new RavenPersistenceFactory(connectionName, new DocumentObjectSerializer(), persistenceOptions).Build());
    }
  }
}