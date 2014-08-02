namespace NEventStore.Persistence.RavenDB
{
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Listeners;
    using Raven.Json.Linq;

    public class CheckpointNumberIncrementListener : IDocumentStoreListener
    {
        private readonly HiLoKeyGenerator _generator;
        private readonly IDocumentStore _store;

        public CheckpointNumberIncrementListener(IDocumentStore store)
        {
            _store = store;
            // http://stackoverflow.com/a/12687849/1010
            _generator = new HiLoKeyGenerator("CheckpointNumber", 1);
        }

        public void AfterStore(string key, object entityInstance, RavenJObject metadata)
        {}

        //TODO: Write couple tests to be sure
        public bool BeforeStore(string key, object entityInstance, RavenJObject metadata, RavenJObject original)
        {
            var commit = entityInstance as RavenCommit;
            if (commit != null && commit.CheckpointNumber == 0)
            {
                commit.CheckpointNumber = _generator.NextId(_store.DatabaseCommands);
                return true;
            }
            return false;
        }
    }
}