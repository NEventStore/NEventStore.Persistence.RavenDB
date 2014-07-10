namespace NEventStore.Persistence.RavenDB.Indexes
{
  using Raven.Client.Indexes;
  using System.Linq;

  public class RavenCommitByCheckpoint : AbstractIndexCreationTask<RavenCommit>
  {
    public RavenCommitByCheckpoint()
    {
      Map = commits => from c in commits select new { c.CheckpointNumber };
      Sort(x => x.CheckpointNumber, Raven.Abstractions.Indexing.SortOptions.Long);
    }
  }
}