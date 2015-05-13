namespace NEventStore.Persistence.RavenDB.Indexes
{
  using System.Linq;
  using Raven.Abstractions.Indexing;
  using Raven.Client.Indexes;

  public class RavenCommitByCheckpoint : AbstractIndexCreationTask<RavenCommit>
  {
    public RavenCommitByCheckpoint()
    {
      Map = commits => from c in commits
                       select new
                       {
                         c.BucketId,
                         c.CheckpointNumber
                       };
      Sort(x => x.CheckpointNumber, SortOptions.Long);
    }
  }
}