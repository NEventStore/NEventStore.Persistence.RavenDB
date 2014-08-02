namespace NEventStore.Persistence.RavenDB.Indexes
{
    using System.Linq;
    using Raven.Abstractions.Indexing;
    using Raven.Client.Indexes;

    public class RavenCommitsByDispatched : AbstractIndexCreationTask<RavenCommit>
    {
        public RavenCommitsByDispatched()
        {
            Map = commits => from c in commits
                             select new
                             {
                                 c.Dispatched,
                                 c.CheckpointNumber
                             };
            Sort(x => x.CheckpointNumber, SortOptions.Long);
        }
    }
}