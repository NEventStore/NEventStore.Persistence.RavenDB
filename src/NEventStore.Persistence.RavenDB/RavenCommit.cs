namespace NEventStore.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using Raven.Imports.Newtonsoft.Json;

    public class RavenCommit
    {
        public string Id { get; set; }
        public string BucketId { get; set; }
        public string StreamId { get; set; }
        public int CommitSequence { get; set; }
        public int StartingStreamRevision { get; set; }
        public int StreamRevision { get; set; }
        public Guid CommitId { get; set; }
        public DateTime CommitStamp { get; set; }

        public IDictionary<string, object> Headers { get; set; }

        [JsonProperty(TypeNameHandling = TypeNameHandling.All)]
        public IList<EventMessage> Payload { get; set; }

        public bool Dispatched { get; set; }

        public long CheckpointNumber { get; set; }
    }
}