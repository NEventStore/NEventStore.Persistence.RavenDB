namespace NEventStore.Persistence.RavenDB
{
  using Raven.Client;
  using Raven.Client.Document;
  using Raven.Client.Listeners;
  using System;
  using System.Transactions;

  public class RavenPersistenceOptions
  {
    public bool ConsistentQueries { get; private set; }
    public string DatabaseName { get; private set; }
    public int PageSize { get; private set; }
    public System.Transactions.TransactionScopeOption ScopeOption { get; private set; }

    private const string defaultDatabaseName = "NEventStore";
    private const TransactionScopeOption defaultScopeOption = TransactionScopeOption.Suppress;
    //These values are considered "safe by default" according to Raven docs
    private const int maxServerPageSize = 1024;
    private const int defaultPageSize = 128;
    //Stale queries perform better
    private const bool defaultConsistentQueries = false;


    public RavenPersistenceOptions()
      : this(defaultDatabaseName, defaultPageSize, defaultConsistentQueries, defaultScopeOption)
    { }
    public RavenPersistenceOptions(string databaseName)
      : this(databaseName, defaultPageSize, defaultConsistentQueries, defaultScopeOption)
    { }

    public RavenPersistenceOptions(string databaseName, int pageSize)
      : this(databaseName, pageSize, defaultConsistentQueries, defaultScopeOption)
    { }

    public RavenPersistenceOptions(string databaseName, int pageSize, bool consistentQueries)
      : this(databaseName, pageSize, consistentQueries, defaultScopeOption)
    { }

    public RavenPersistenceOptions(string databaseName, int pageSize, bool consistentQueries, TransactionScopeOption scopeOption)
    {
      PageSize = (pageSize > maxServerPageSize) ? maxServerPageSize : pageSize;
      DatabaseName = databaseName;
      ConsistentQueries = consistentQueries;
      ScopeOption = scopeOption;
    }

    internal IDocumentStore GetDocumentStore(string connectionName)
    {
      if (string.IsNullOrEmpty(connectionName))
        throw new ArgumentNullException("connectionName");
      var store = new DocumentStore();
      store.ConnectionStringName = connectionName;
      store.DefaultDatabase = !string.IsNullOrEmpty(DatabaseName) ? DatabaseName : defaultDatabaseName;
      store.Initialize();
      store.RegisterListener(new CheckpointNumberIncrementListener(store));
      return store;
    }
  }
}
