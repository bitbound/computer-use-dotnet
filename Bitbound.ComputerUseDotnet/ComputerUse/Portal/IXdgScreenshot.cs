using Tmds.DBus;

namespace Bitbound.ComputerUseDotnet.ComputerUse.Portal;

/// <summary>org.freedesktop.portal.Screenshot: one-shot capture of the entire screen.</summary>
[DBusInterface("org.freedesktop.portal.Screenshot")]
internal interface IXdgScreenshot : IDBusObject
{
  Task<ObjectPath> ScreenshotAsync(string parentWindow, IDictionary<string, object> options);
}
