namespace NEventStore.Persistence.RavenDB.Indexes
{
  using Raven.Client.Indexes;
  using System.Linq;

  public class RavenCommitByDate : AbstractIndexCreationTask<RavenCommit>
  {
    public RavenCommitByDate()
    {
      Map = commits => from c in commits select new { c.BucketId, c.CommitStamp };
    }
  }
}