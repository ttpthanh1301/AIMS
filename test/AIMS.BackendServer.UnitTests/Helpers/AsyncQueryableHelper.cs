using System.Linq.Expressions;

namespace AIMS.BackendServer.UnitTests.Helpers;

// ── Implement thêm IOrderedQueryable để hỗ trợ OrderBy() ──────
public class AsyncQueryable<T> : IOrderedQueryable<T>, IAsyncEnumerable<T>
{
    private readonly IQueryable<T> _inner;

    public AsyncQueryable(IEnumerable<T> data)
        => _inner = data.AsQueryable();

    public AsyncQueryable(IQueryable<T> query)
        => _inner = query;

    public Type ElementType => _inner.ElementType;
    public Expression Expression => _inner.Expression;
    public IQueryProvider Provider => new AsyncQueryProvider<T>(_inner.Provider);

    public IEnumerator<T> GetEnumerator()
        => _inner.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        => _inner.GetEnumerator();

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken ct = default)
        => new AsyncEnumeratorWrapper<T>(_inner.GetEnumerator());
}

public class AsyncEnumeratorWrapper<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public AsyncEnumeratorWrapper(IEnumerator<T> inner)
        => _inner = inner;

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync()
        => ValueTask.FromResult(_inner.MoveNext());

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }
}

// ── Provider hỗ trợ cả CreateQuery thường và IOrderedQueryable ─
public class AsyncQueryProvider<T> : IQueryProvider
{
    private readonly IQueryProvider _inner;

    public AsyncQueryProvider(IQueryProvider inner)
        => _inner = inner;

    public IQueryable CreateQuery(Expression expression)
        => new AsyncQueryable<T>(
               _inner.CreateQuery<T>(expression));

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => new AsyncQueryable<TElement>(         // ← Trả về AsyncQueryable
               _inner.CreateQuery<TElement>(expression));

    public object? Execute(Expression expression)
        => _inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression)
        => _inner.Execute<TResult>(expression);
}