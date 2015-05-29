﻿namespace NEventStore.Persistence.RavenDB
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Transactions;
  using NEventStore.Logging;
  using NEventStore.Serialization;
  using Raven.Client;
  using NEventStore.Persistence.RavenDB.Indexes;
  using System.Net;
  using Raven.Client.Exceptions;
  using System.Linq.Expressions;
  using Raven.Client.Indexes;
  using Raven.Abstractions.Data;
  using Raven.Json.Linq;
  using Raven.Abstractions.Commands;

  public class RavenPersistenceEngine : IPersistStreams
  {
    private int _initialized;
    private static readonly ILog Logger = LogFactory.BuildLogger(typeof(RavenPersistenceEngine));
    private readonly TransactionScopeOption _scopeOption;
    private readonly bool _consistentQueries;
    private readonly TimeSpan? _consistencyTimeout;    
    private readonly int _pageSize;
    private readonly IDocumentStore _store;
    private readonly IDocumentSerializer _serializer;

      public RavenPersistenceEngine(IDocumentStore store, IDocumentSerializer serializer, RavenPersistenceOptions options)
    {
      if (store == null)
        throw new ArgumentNullException("store");
      if (serializer == null)
        throw new ArgumentNullException("serializer");
      if (options == null)
        throw new ArgumentNullException("options");
      _store = store;
      _serializer = serializer;
      _consistentQueries = options.ConsistentQueries;
      _consistencyTimeout = options.ConsistencyTimeout;
      _pageSize = options.PageSize;
      _scopeOption = options.ScopeOption;
    }

    public virtual void Initialize()
    {
      if (Interlocked.Increment(ref _initialized) > 1)
        return;

      Logger.Debug(Messages.InitializingStorage);

      TryRaven(() =>
      {
        using (TransactionScope scope = OpenCommandScope())
        {
          new EventStoreDocumentsByEntityName().Execute(_store);
          new RavenCommitByCheckpoint().Execute(_store);
          new RavenCommitByDate().Execute(_store);
          new RavenCommitByRevisionRange().Execute(_store);
          new RavenCommitsByDispatched().Execute(_store);
          new RavenSnapshotByStreamIdAndRevision().Execute(_store);
          new RavenStreamHeadBySnapshotAge().Execute(_store);
          scope.Complete();
        }
        return true;
      });
    }

    public IDocumentStore Store
    {
      get { return _store; }
    }

    protected virtual T TryRaven<T>(Func<T> callback)
    {
      try
      {
        return callback();
      }
      catch (WebException e)
      {
        Logger.Warn(Messages.StorageUnavailable);
        throw new StorageUnavailableException(e.Message, e);
      }
      catch (NonUniqueObjectException e)
      {
        Logger.Warn(Messages.DuplicateCommitDetected);
        throw new DuplicateCommitException(e.Message, e);
      }
      catch (Raven.Abstractions.Exceptions.ConcurrencyException)
      {
        Logger.Warn(Messages.ConcurrentWriteDetected);
        throw;
      }
      catch (ObjectDisposedException)
      {
        Logger.Warn(Messages.StorageAlreadyDisposed);
        throw;
      }
      catch (Exception e)
      {
        Logger.Error(Messages.StorageThrewException, e.GetType());
        throw new StorageException(e.Message, e);
      }
    }

    public bool IsDisposed
    {
      get { return _store.WasDisposed; }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposing || IsDisposed)
        return;
      Logger.Debug(Messages.ShuttingDownPersistence);
      _store.Dispose();
    }

    #region Query Helpers
    private IEnumerable<ICommit> QueryCommits<TIndex>(Expression<Func<RavenCommit, bool>> query, Expression<Func<RavenCommit, object>> orderBy) where TIndex : AbstractIndexCreationTask, new()
    {
      return Query<RavenCommit, TIndex>(query, orderBy).Select(x => x.ToCommit(_serializer));
    }

    private IEnumerable<T> Query<T, TIndex>(Expression<Func<T, bool>> where, Expression<Func<T, object>> orderBy, bool orderDesc = false)
    where TIndex : AbstractIndexCreationTask, new()
    {
      return new ResetableEnumerable<T>(() => PagedQuery<T, TIndex>(where, orderBy, orderDesc));
    }

    private IEnumerable<T> PagedQuery<T, TIndex>(Expression<Func<T, bool>> where, Expression<Func<T, object>> orderBy, bool orderDesc = false) where TIndex : AbstractIndexCreationTask, new()
    {
      int total = 0;
      RavenQueryStatistics stats;
      do
      {
        using (IDocumentSession session = _store.OpenSession())
        {
          int requestsForSession = 0;
          do
          {
            T[] docs = PerformQuery<T, TIndex>(session, where, orderBy, orderDesc, total, _pageSize, out stats);
            total += docs.Length;
            requestsForSession++;
            foreach (var d in docs)
              yield return d;
          } while (total < stats.TotalResults && requestsForSession < session.Advanced.MaxNumberOfRequestsPerSession);
        }
      } while (total < stats.TotalResults);
    }

    private T[] PerformQuery<T, TIndex>(
        IDocumentSession session, Expression<Func<T, bool>> where, Expression<Func<T, object>> orderBy, bool orderDesc, int skip, int take, out RavenQueryStatistics stats)
        where TIndex : AbstractIndexCreationTask, new()
    {
      try
      {
        using (TransactionScope scope = OpenCommandScope())
        {
          IQueryable<T> query = session.Query<T, TIndex>().Customize(x =>
          {
              if (!_consistentQueries) return;
              
              if(_consistencyTimeout.HasValue)
                  x.WaitForNonStaleResults(_consistencyTimeout.Value);
              else
                  x.WaitForNonStaleResults();
          })
          .Statistics(out stats)
          .Where(where);
          if (orderDesc)
            query = query.OrderByDescending(orderBy);
          else
            query = query.OrderBy(orderBy);
          var results = query.Skip(skip).Take(take).ToArray();
          scope.Complete();
          return results;
        }
      }
      catch (WebException e)
      {
        Logger.Warn(Messages.StorageUnavailable);
        throw new StorageUnavailableException(e.Message, e);
      }
      catch (ObjectDisposedException)
      {
        Logger.Warn(Messages.StorageAlreadyDisposed);
        throw;
      }
      catch (Exception e)
      {
        Logger.Error(Messages.StorageThrewException, e.GetType());
        throw new StorageException(e.Message, e);
      }
    }
    #endregion

    public virtual IEnumerable<ICommit> GetFrom(string bucketId, string streamId, int minRevision, int maxRevision)
    {
      Logger.Debug(Messages.GettingAllCommitsBetween, streamId, bucketId, minRevision, maxRevision);
      return QueryCommits<RavenCommitByRevisionRange>(x => x.BucketId == bucketId && x.StreamId == streamId && x.StreamRevision >= minRevision && x.StartingStreamRevision <= maxRevision, x => x.CommitSequence);

    }

    public virtual IEnumerable<ICommit> GetFrom(string bucketId, DateTime start)
    {
      Logger.Debug(Messages.GettingAllCommitsFrom, start, bucketId);
      return QueryCommits<RavenCommitByDate>(x => x.BucketId == bucketId && x.CommitStamp >= start, x => x.CommitStamp);
    }
    public IEnumerable<ICommit> GetFrom(string bucketId, string checkpointToken)
    {
      var intCheckpoint = LongCheckpoint.Parse(checkpointToken);
      Logger.Debug(Messages.GettingAllCommitsFromBucketAndCheckpoint, bucketId, intCheckpoint.Value);
      return QueryCommits<RavenCommitByCheckpoint>(x => x.BucketId == bucketId && x.CheckpointNumber > intCheckpoint.LongValue, x => x.CheckpointNumber);
    }

    public ICheckpoint GetCheckpoint(string checkpointToken)
    {
      return LongCheckpoint.Parse(checkpointToken);
    }

    public virtual IEnumerable<ICommit> GetFromTo(string bucketId, DateTime start, DateTime end)
    {
      Logger.Debug(Messages.GettingAllCommitsFromTo, start, end);
      return QueryCommits<RavenCommitByDate>(x => x.BucketId == bucketId && x.CommitStamp >= start && x.CommitStamp < end, x => x.CommitStamp); //.ThenBy(x => x.StreamId).ThenBy(x => x.CommitSequence);
    }

    public virtual IEnumerable<ICommit> GetUndispatchedCommits()
    {
      Logger.Debug(Messages.GettingUndispatchedCommits);
      return QueryCommits<RavenCommitsByDispatched>(x => !x.Dispatched, x => x.CheckpointNumber);
    }

    public virtual void MarkCommitAsDispatched(ICommit commit)
    {
      if (commit == null)
        throw new ArgumentNullException("commit");

      var data = new PatchCommandData
      {
        Key = commit.ToRavenCommitId(),
        Patches = new[] { new PatchRequest { Type = PatchCommandType.Set, Name = "Dispatched", Value = RavenJToken.Parse("true") } }
      };

      Logger.Debug(Messages.MarkingCommitAsDispatched, commit.CommitId, commit.BucketId);

      TryRaven(() =>
      {
        using (TransactionScope scope = OpenCommandScope())
        using (IDocumentSession session = _store.OpenSession())
        {
          session.Advanced.DocumentStore.DatabaseCommands.Batch(new[] { data });
          session.SaveChanges();
          scope.Complete();
          return true;
        }
      });
    }

    private bool HasDocs(string index, IndexQuery query)
    {
      while (_store.DatabaseCommands.GetStatistics().StaleIndexes.Contains(index))
        Thread.Sleep(50);
      return _store.DatabaseCommands.Query(index, query, null, true).TotalResults != 0;
    }

    private void PurgeStorage(IDocumentSession session)
    {
      Func<Type, string> getTagCondition = t => "Tag:" + session.Advanced.DocumentStore.Conventions.GetTypeTagName(t);
      var query = new IndexQuery { Query = String.Format("({0} OR {1} OR {2})", getTagCondition(typeof(RavenCommit)), getTagCondition(typeof(RavenSnapshot)), getTagCondition(typeof(RavenStreamHead))) };
      while (HasDocs(EventStoreDocumentsByEntityName.IndexNameValue, query))
        session.Advanced.DocumentStore.DatabaseCommands.DeleteByIndex(EventStoreDocumentsByEntityName.IndexNameValue, query, 
            new BulkOperationOptions { AllowStale = true });
    }

    private void PurgeBucket(IDocumentSession session, string bucketId)
    {
      //TODO: To test -> Edited index by adding bucketId and streamId
      Func<Type, string> getTagCondition = t => "Tag:" + session.Advanced.DocumentStore.Conventions.GetTypeTagName(t);
      var query = new IndexQuery { Query = String.Format("({0} OR {1} OR {2}) AND (BucketId: {3})", getTagCondition(typeof(RavenCommit)), getTagCondition(typeof(RavenSnapshot)), getTagCondition(typeof(RavenStreamHead)), bucketId) };
      while (HasDocs(EventStoreDocumentsByEntityName.IndexNameValue, query))
          session.Advanced.DocumentStore.DatabaseCommands.DeleteByIndex(EventStoreDocumentsByEntityName.IndexNameValue, query, 
              new BulkOperationOptions { AllowStale = true });
    }

    public virtual void Purge()
    {
      Logger.Warn(Messages.PurgingStorage);
      TryRaven(() =>
      {
        using (TransactionScope scope = OpenCommandScope())
        using (IDocumentSession session = _store.OpenSession())
        {
          PurgeStorage(session);
          session.SaveChanges();
          scope.Complete();
          return true;
        }
      });
    }

    public virtual void Purge(string bucketId)
    {
      Logger.Warn(Messages.PurgingBucket, bucketId);
      TryRaven(() =>
      {
        using (TransactionScope scope = OpenCommandScope())
        using (IDocumentSession session = _store.OpenSession())
        {
          PurgeBucket(session, bucketId);
          session.SaveChanges();
          scope.Complete();
          return true;
        }
      });
    }

    public virtual void Drop()
    {
      Purge();
    }

    public virtual void DeleteStream(string bucketId, string streamId)
    {
      Logger.Warn(Messages.DeletingStream, streamId, bucketId);
      TryRaven(() =>
      {
        using (TransactionScope scope = OpenCommandScope())
        using (IDocumentSession session = _store.OpenSession())
        {
          DeleteStream(session, bucketId, streamId);
          session.SaveChanges();
          scope.Complete();
          return true;
        }
      });
    }

    private void DeleteStream(IDocumentSession session, string bucketId, string streamId)
    {
      //TODO: To test -> Edited index by adding bucketId and streamId
      Func<Type, string> getTagCondition = t => "Tag:" + session.Advanced.DocumentStore.Conventions.GetTypeTagName(t);
      var query = new IndexQuery { Query = String.Format("({0} OR {1} OR {2}) AND BucketId: {3} AND StreamId: {4}", getTagCondition(typeof(RavenCommit)), getTagCondition(typeof(RavenSnapshot)), getTagCondition(typeof(RavenStreamHead)), bucketId, streamId) };
      while (HasDocs(EventStoreDocumentsByEntityName.IndexNameValue, query))
          session.Advanced.DocumentStore.DatabaseCommands.DeleteByIndex(EventStoreDocumentsByEntityName.IndexNameValue, query, 
              new BulkOperationOptions { AllowStale = true });
    }

    public virtual IEnumerable<ICommit> GetFrom(string checkpointToken)
    {
      Logger.Debug(Messages.GettingAllCommitsFromCheckpoint, checkpointToken);
      LongCheckpoint checkpoint = LongCheckpoint.Parse(checkpointToken);
      return QueryCommits<RavenCommitByCheckpoint>(x => x.CheckpointNumber > checkpoint.LongValue, x => x.CheckpointNumber);
    }

    public virtual ICommit Commit(CommitAttempt attempt)
    {
      Logger.Debug(Messages.AttemptingToCommit, attempt.Events.Count, attempt.StreamId, attempt.CommitSequence, attempt.BucketId);
      try
      {
        return TryRaven(() =>
        {
          var doc = attempt.ToRavenCommit(_serializer);
          using (TransactionScope scope = OpenCommandScope())
          using (IDocumentSession session = _store.OpenSession())
          {
            session.Advanced.UseOptimisticConcurrency = true;
            session.Store(doc);
            session.SaveChanges();
            scope.Complete();
          }
          Logger.Debug(Messages.CommitPersisted, attempt.CommitId, attempt.BucketId);
          SaveStreamHead(attempt.ToRavenStreamHead());
          return doc.ToCommit(_serializer);
        });
      }
      catch (Raven.Abstractions.Exceptions.ConcurrencyException)
      {
        RavenCommit savedCommit = LoadSavedCommit(attempt);
        if (savedCommit.CommitId == attempt.CommitId)
          throw new DuplicateCommitException();
        Logger.Debug(Messages.ConcurrentWriteDetected);
        throw new ConcurrencyException();
      }

    }

    private void SaveStreamHead(RavenStreamHead streamHead)
    {
      if (_consistentQueries)
        SaveStreamHeadAsync(streamHead);
      else
        ThreadPool.QueueUserWorkItem(x => SaveStreamHeadAsync(streamHead), null);
    }

    private void SaveStreamHeadAsync(RavenStreamHead updated)
    {
      TryRaven(() =>
      {
        using (TransactionScope scope = OpenCommandScope())
        using (IDocumentSession session = _store.OpenSession())
        {
          RavenStreamHead current = session.Load<RavenStreamHead>(RavenStreamHead.GetStreamHeadId(updated.BucketId, updated.StreamId)) ?? updated;
          current.HeadRevision = updated.HeadRevision;
          if (updated.SnapshotRevision > 0)
            current.SnapshotRevision = updated.SnapshotRevision;
          session.Advanced.UseOptimisticConcurrency = false;
          session.Store(current);
          session.SaveChanges();
          scope.Complete(); // if this fails it's no big deal, stream heads can be updated whenever
        }
        return true;
      });
    }

    private RavenCommit LoadSavedCommit(CommitAttempt attempt)
    {
      Logger.Debug(Messages.DetectingConcurrency);
      return TryRaven(() =>
      {
        using (TransactionScope scope = OpenQueryScope())
        using (IDocumentSession session = _store.OpenSession())
        {
          var commit = session.Load<RavenCommit>(attempt.ToRavenCommitId());
          scope.Complete();
          return commit;
        }
      });
    }

    public virtual ISnapshot GetSnapshot(string bucketId, string streamId, int maxRevision)
    {
      Logger.Debug(Messages.GettingRevision, streamId, maxRevision);
      return Query<RavenSnapshot, RavenSnapshotByStreamIdAndRevision>(x => x.BucketId == bucketId && x.StreamId == streamId && x.StreamRevision <= maxRevision, x => x.StreamRevision, true)
        .FirstOrDefault()
        .ToSnapshot(_serializer);
    }

    public virtual bool AddSnapshot(ISnapshot snapshot)
    {
      if (snapshot == null)
      {
        return false;
      }
      Logger.Debug(Messages.AddingSnapshot, snapshot.StreamId, snapshot.BucketId, snapshot.StreamRevision);
      try
      {
        return TryRaven(() =>
        {
          using (TransactionScope scope = OpenCommandScope())
          using (IDocumentSession session = _store.OpenSession())
          {
            RavenSnapshot ravenSnapshot = snapshot.ToRavenSnapshot(_serializer);
            session.Store(ravenSnapshot);
            session.SaveChanges();
            scope.Complete();
          }
          SaveStreamHead(snapshot.ToRavenStreamHead());
          return true;
        });
      }
      catch (Raven.Abstractions.Exceptions.ConcurrencyException)
      {
        return false;
      }
    }

    public virtual IEnumerable<IStreamHead> GetStreamsToSnapshot(string bucketId, int maxThreshold)
    {
      Logger.Debug(Messages.GettingStreamsToSnapshot, bucketId);
      return Query<RavenStreamHead, RavenStreamHeadBySnapshotAge>(s => s.BucketId == bucketId && s.SnapshotAge >= maxThreshold, x => x.StreamId).Select(s => s.ToStreamHead());
    }

    protected virtual TransactionScope OpenCommandScope()
    {
      return new TransactionScope(_scopeOption);
    }

    protected virtual TransactionScope OpenQueryScope()
    {
      return OpenCommandScope() ?? new TransactionScope(TransactionScopeOption.Suppress);
    }
  }
}
