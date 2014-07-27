namespace NEventStore.Persistence.RavenDB
{
  using Raven.Client;
  using Raven.Client.Document;
  using Raven.Client.Listeners;
  using Raven.Json.Linq;

  public class CheckpointNumberIncrementListener : IDocumentStoreListener
  {
    HiLoKeyGenerator _generator;
    IDocumentStore _store;
    public CheckpointNumberIncrementListener(IDocumentStore store)
    {
      this._store = store;
      _generator = new HiLoKeyGenerator("CheckpointNumber", 1);
    }

    public void AfterStore(string key, object entityInstance, RavenJObject metadata)
    {
    }

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
