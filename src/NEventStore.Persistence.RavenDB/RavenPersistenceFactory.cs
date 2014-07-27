namespace NEventStore.Persistence.RavenDB
{
  using NEventStore.Serialization;

  public class RavenPersistenceFactory : IPersistenceFactory
  {
    protected readonly string connectionName;
    protected readonly RavenPersistenceOptions options;
    protected readonly IDocumentSerializer serializer;

    public RavenPersistenceFactory(string connectionName, IDocumentSerializer serializer, RavenPersistenceOptions options)
    {
      this.options = options;
      this.connectionName = connectionName;
      this.serializer = serializer;
    }

    public virtual IPersistStreams Build()
    {
      return new RavenPersistenceEngine(options.GetDocumentStore(connectionName), serializer, options);
    }
  }
}