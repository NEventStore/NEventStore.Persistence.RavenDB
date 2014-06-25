namespace NEventStore.Persistence.RavenDB
{
  using NEventStore.Serialization;
  using Raven.Client;
  using Raven.Client.Document;
  using System;

  public class RavenPersistenceFactory : IPersistenceFactory
  {
    #region OldVersion
    //private readonly RavenConfiguration _config;

    //public RavenPersistenceFactory(RavenConfiguration config)
    //{
    //    _config = config;
    //}

    //public virtual IPersistStreams Build()
    //{
    //  IDocumentStore store = GetStore();
    //  return new RavenPersistenceEngine(store, _config);
    //}

    //#pragma warning disable 612,618
    //    protected virtual IDocumentStore GetStore()
    //    {
    //      var store = new DocumentStore();

    //      if (!string.IsNullOrEmpty(_config.ConnectionName))
    //      {
    //        store.ConnectionStringName = _config.ConnectionName;
    //      }

    //      if (_config.Url != null)
    //      {
    //        store.Url = _config.Url.ToString();
    //      }

    //      if (!string.IsNullOrEmpty(_config.DefaultDatabase))
    //      {
    //        store.DefaultDatabase = _config.DefaultDatabase;
    //      }

    //      store.Initialize();

    //      return store;
    //    }
    //#pragma warning restore 612,618
    #endregion

    private readonly string connectionName;
    private readonly RavenPersistenceOptions options;
    private readonly IDocumentSerializer serializer;

    public RavenPersistenceFactory(string connectionName, IDocumentSerializer serializer, RavenPersistenceOptions options)
    {
      this.options = options;
      this.connectionName = connectionName;
      this.serializer = serializer;
    }

    public IPersistStreams Build()
    {
      return new RavenPersistenceEngine(options.GetDocumentStore(connectionName), serializer, options);
    }
  }
}