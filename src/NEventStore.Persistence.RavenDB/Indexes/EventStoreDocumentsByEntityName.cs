namespace NEventStore.Persistence.RavenDB.Indexes
{
    using Raven.Abstractions.Indexing;
    using Raven.Client.Indexes;

    public class EventStoreDocumentsByEntityName : AbstractIndexCreationTask
    {
        public const string IndexNameValue = "EventStoreDocumentsByEntityName";

        public override string IndexName
        {
            get { return IndexNameValue; }
        }

        public override IndexDefinition CreateIndexDefinition()
        {
            return new IndexDefinition
            {
                // Can't use Linq because this index is not against a specific document type.
                Map = @"from doc in docs let Tag = doc[""@metadata""][""Raven-Entity-Name""]
                        where  Tag != null 
                        select new { 
                            Tag,
                            LastModified = (DateTime)doc[""@metadata""][""Last-Modified""],
                            Partition = doc.Partition ?? null,
                            BucketId = doc.BucketId ?? null,
                            StreamId = doc.StreamId ?? null
                        };",
                Indexes =
                {
                    {"Tag", FieldIndexing.NotAnalyzed}
                },
                Stores =
                {
                    {"Tag", FieldStorage.No},
                    {"LastModified", FieldStorage.No},
                    {"Partition", FieldStorage.No},
                    {"BucketId", FieldStorage.No},
                    {"StreamId", FieldStorage.No}
                }
            };
        }
    }
}