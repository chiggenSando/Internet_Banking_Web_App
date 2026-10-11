using Mcba.AdminPortal.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Mcba.AdminPortal.Tests;

// Gives a controller the session and TempData it would normally get from the ASP.NET Core pipeline.
public static class ControllerTestHelper
{
    public static T WithSession<T>(this T controller) where T : Controller
    {
        var httpContext = new DefaultHttpContext { Session = new TestSession() };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
        return controller;
    }

    public static T LoggedInAsAdmin<T>(this T controller) where T : Controller
    {
        controller.WithSession().HttpContext.Session.SetString(LoginController.SessionKey, "admin");
        return controller;
    }

    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _values = new();

        public string Id => "test-session";
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _values.Keys;

        public void Clear() => _values.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _values.Remove(key);
        public void Set(string key, byte[] value) => _values[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _values.TryGetValue(key, out value!);
    }

    private class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
