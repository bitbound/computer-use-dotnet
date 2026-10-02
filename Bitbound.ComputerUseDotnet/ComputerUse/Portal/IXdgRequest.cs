using Tmds.DBus;

namespace Bitbound.ComputerUseDotnet.ComputerUse.Portal;

/// <summary>org.freedesktop.portal.Request: a portal request completes via the Response signal.</summary>
[DBusInterface("org.freedesktop.portal.Request")]
internal interface IXdgRequest : IDBusObject
{
  Task CloseAsync();

  Task<IDisposable> WatchResponseAsync(
    Action<(uint response, IDictionary<string, object> results)> handler,
    Action<Exception>? onError = null);
}
