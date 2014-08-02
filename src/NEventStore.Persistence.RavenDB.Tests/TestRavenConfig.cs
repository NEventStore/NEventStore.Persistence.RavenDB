namespace NEventStore.Persistence.RavenDB.Tests
{
    using System.Transactions;
    using NEventStore.Serialization;

    public static class TestRavenConfig
    {
        public static readonly DocumentObjectSerializer Serializer = new DocumentObjectSerializer();
        public const TransactionScopeOption ScopeOption = TransactionScopeOption.Suppress;
        public const bool ConsistentQueries = true; // helps tests pass consistently
        public const int PageSize = 10; // smaller values help bring out bugs
        public const string ConnectionName = "Raven";
    }
}