namespace NEventStore.Persistence.RavenDB.Indexes
{
  using Raven.Client.Indexes;
  using System.Linq;

  public class RavenStreamHeadBySnapshotAge : AbstractIndexCreationTask<RavenStreamHead>
  {
    public RavenStreamHeadBySnapshotAge()
    {
      Map = snapshots => from s in snapshots select new { s.BucketId, SnapshotAge = s.HeadRevision - s.SnapshotRevision, s.StreamId };
    }
  }
}