using Microsoft.AspNetCore.Http;

namespace Mcba.CustomerWebsite.Tests.Common;

// In-memory ISession so controllers can read and write HttpContext.Session in tests.
public class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _values = new();

    public bool IsAvailable => true;
    public string Id => "test-session";
    public IEnumerable<string> Keys => _values.Keys;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public bool TryGetValue(string key, out byte[] value) => _values.TryGetValue(key, out value);
    public void Set(string key, byte[] value) => _values[key] = value;
    public void Remove(string key) => _values.Remove(key);
    public void Clear() => _values.Clear();
}