namespace NEventStore.Persistence.RavenDB.Indexes
{
  using Raven.Client.Indexes;
  using System.Linq;

  public class RavenCommitsByDispatched : AbstractIndexCreationTask<RavenCommit>
  {
    public RavenCommitsByDispatched()
    {
      Map = commits => from c in commits select new { c.Dispatched, c.CheckpointNumber };
      Sort(x => x.CheckpointNumber, Raven.Abstractions.Indexing.SortOptions.Long);
    }
  }
}