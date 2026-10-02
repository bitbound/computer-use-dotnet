using Tmds.DBus;

namespace Bitbound.ComputerUseDotnet.ComputerUse.Portal;

/// <summary>
/// org.freedesktop.portal.RemoteDesktop: simulate keyboard/pointer input on Wayland.
/// Coordinates for absolute motion are normalized (0..1) within a stream.
/// </summary>
[DBusInterface("org.freedesktop.portal.RemoteDesktop")]
internal interface IXdgRemoteDesktop : IDBusObject
{
  Task<ObjectPath> CreateSessionAsync(IDictionary<string, object> options);

  Task<ObjectPath> SelectDevicesAsync(ObjectPath sessionHandle, IDictionary<string, object> options);

  Task<ObjectPath> StartAsync(ObjectPath sessionHandle, string parentWindow, IDictionary<string, object> options);

  Task NotifyPointerMotionAsync(ObjectPath sessionHandle, IDictionary<string, object> options, double dx, double dy);

  Task NotifyPointerMotionAbsoluteAsync(ObjectPath sessionHandle, IDictionary<string, object> options, uint stream, double x, double y);

  Task NotifyPointerButtonAsync(ObjectPath sessionHandle, IDictionary<string, object> options, uint button, uint state);

  Task NotifyKeyboardKeycodeAsync(ObjectPath sessionHandle, IDictionary<string, object> options, uint keycode, uint state);

  Task NotifyPointerAxisDiscreteAsync(ObjectPath sessionHandle, IDictionary<string, object> options, uint axis, int steps);
}
