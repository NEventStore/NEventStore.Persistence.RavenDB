namespace NEventStore.Persistence.RavenDB.Tests
{
  using System;
  using System.Transactions;
  using NEventStore.Persistence.AcceptanceTests;
  using NEventStore.Serialization;
  using Raven.Database.Config;

  public static class TestRavenConfig
  {
    //public static RavenConfiguration GetDefaultConfig()
    //{
    //  return new RavenConfiguration
    //  {
    //    Serializer = new DocumentObjectSerializer(),
    //    ScopeOption = TransactionScopeOption.Suppress,
    //    ConsistentQueries = true, // helps tests pass consistently
    //    RequestedPageSize = Int32.Parse("pageSize".GetSetting() ?? "10"), // smaller values help bring out bugs
    //    MaxServerPageSize = Int32.Parse("serverPageSize".GetSetting() ?? "1024"), // raven default
    //    ConnectionName = "Raven"
    //  };
    //}


    public static DocumentObjectSerializer Serializer = new DocumentObjectSerializer();
    public static TransactionScopeOption ScopeOption = TransactionScopeOption.Suppress;
    public static bool ConsistentQueries = true; // helps tests pass consistently
    public static int PageSize = 10; // smaller values help bring out bugs
    public static string ConnectionName = "Raven";

  }
}