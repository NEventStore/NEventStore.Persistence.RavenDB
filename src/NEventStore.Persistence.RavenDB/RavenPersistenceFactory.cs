namespace NEventStore.Persistence.RavenDB
{
    using NEventStore.Serialization;

    public class RavenPersistenceFactory : IPersistenceFactory
    {
        private readonly string _connectionName;
        private readonly RavenPersistenceOptions _options;
        private readonly IDocumentSerializer _serializer;

        public RavenPersistenceFactory(string connectionName, IDocumentSerializer serializer, RavenPersistenceOptions options)
        {
            _options = options;
            _connectionName = connectionName;
            _serializer = serializer;
        }

        public RavenPersistenceOptions Options
        {
            get { return _options; }
        }

        public IDocumentSerializer Serializer
        {
            get { return _serializer; }
        }

        public virtual IPersistStreams Build()
        {
            return new RavenPersistenceEngine(_options.GetDocumentStore(_connectionName), _serializer, _options);
        }
    }
}