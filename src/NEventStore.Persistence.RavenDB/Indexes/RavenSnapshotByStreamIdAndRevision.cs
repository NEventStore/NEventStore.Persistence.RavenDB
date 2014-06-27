namespace NEventStore.Persistence.RavenDB.Indexes
{
  using Raven.Client.Indexes;
  using System.Linq;

  public class RavenSnapshotByStreamIdAndRevision : AbstractIndexCreationTask<RavenSnapshot>
  {
    public RavenSnapshotByStreamIdAndRevision()
    {
      Map = snapshots => from s in snapshots select new { s.BucketId, s.StreamId, s.StreamRevision };
    }
  }
}