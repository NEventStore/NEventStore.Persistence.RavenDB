namespace NEventStore.Persistence.RavenDB
{
    using System;
    using System.Transactions;
    using Raven.Client;
    using Raven.Client.Document;

    public class RavenPersistenceOptions
    {
        private const string DefaultDatabaseName = "NEventStore";
        private const TransactionScopeOption DefaultScopeOption = TransactionScopeOption.Suppress;
        //These values are considered "safe by default" according to Raven docs
        private const int MaxServerPageSize = 1024;
        private const int DefaultPageSize = 128;
        //Stale queries perform better
        private const bool DefaultConsistentQueries = false;
        private readonly int _pageSize;
        private readonly string _databaseName;
        private readonly bool _consistentQueries;
        private TransactionScopeOption _scopeOption;

        public RavenPersistenceOptions(
            int pageSize = DefaultPageSize,
            bool consistentQueries = DefaultConsistentQueries,
            TransactionScopeOption scopeOption = DefaultScopeOption,
            string databaseName = DefaultDatabaseName)
        {
            _pageSize = (pageSize > MaxServerPageSize) ? MaxServerPageSize : pageSize;
            _databaseName = databaseName;
            _consistentQueries = consistentQueries;
            _scopeOption = scopeOption;
        }

        public bool ConsistentQueries
        {
            get { return _consistentQueries; }
        }

        public string DatabaseName
        {
            get { return _databaseName; }
        }

        public int PageSize
        {
            get { return _pageSize; }
        }

        public TransactionScopeOption ScopeOption
        {
            get { return _scopeOption; }
        }

        internal IDocumentStore GetDocumentStore(string connectionName)
        {
            if (string.IsNullOrEmpty(connectionName))
            {
                throw new ArgumentNullException("connectionName");
            }
            var store = new DocumentStore();
            store.ConnectionStringName = connectionName;
            if (string.IsNullOrEmpty(store.DefaultDatabase))
            {
                store.DefaultDatabase = !string.IsNullOrEmpty(DatabaseName) ? DatabaseName : DefaultDatabaseName;
            }
            store.Initialize();
            store.RegisterListener(new CheckpointNumberIncrementListener(store));
            return store;
        }
    }
}