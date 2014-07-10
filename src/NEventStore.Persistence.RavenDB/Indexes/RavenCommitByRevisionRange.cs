namespace NEventStore.Persistence.RavenDB.Indexes
{
  using System.Linq;
  using Raven.Client.Indexes;

  public class RavenCommitByRevisionRange : AbstractIndexCreationTask<RavenCommit>
  {
    public RavenCommitByRevisionRange()
    {
      Map = commits => from c in commits select new { c.BucketId, c.StreamId, c.StartingStreamRevision, c.StreamRevision, c.CommitSequence };
      Sort(x => x.CommitSequence, Raven.Abstractions.Indexing.SortOptions.Int);
    }
  }
}