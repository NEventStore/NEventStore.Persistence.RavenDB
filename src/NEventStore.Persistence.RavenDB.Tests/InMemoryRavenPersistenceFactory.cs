namespace NEventStore.Persistence.RavenDB.Tests
{
    using NEventStore.Serialization;
    using Raven.Client.Embedded;

    public class InMemoryRavenPersistenceFactory : RavenPersistenceFactory
    {
        public InMemoryRavenPersistenceFactory(string connectionName, IDocumentSerializer serializer,
            RavenPersistenceOptions options)
            : base(connectionName, serializer, options)
        {}

        public override IPersistStreams Build()
        {
            var embeddedStore = new EmbeddableDocumentStore();
            embeddedStore.Configuration.RunInMemory = true;
            embeddedStore.Configuration.Storage.Voron.AllowOn32Bits = true;
            embeddedStore.RegisterListener(new CheckpointNumberIncrementListener(embeddedStore));
            embeddedStore.Initialize();
            return new RavenPersistenceEngine(embeddedStore, Serializer, Options);
        }
    }
}