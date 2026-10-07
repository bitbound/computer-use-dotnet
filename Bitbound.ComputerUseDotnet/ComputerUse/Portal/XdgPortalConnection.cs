using Microsoft.Extensions.Logging;
using Tmds.DBus;

namespace Bitbound.ComputerUseDotnet.ComputerUse.Portal;

/// <summary>
/// Owns the session-bus connection to org.freedesktop.portal.Desktop and the
/// "subscribe to Request::Response, then invoke, then await" request pattern.
/// </summary>
internal sealed class XdgPortalConnection(ILogger logger) : IDisposable
{
  public const string PortalBusName = "org.freedesktop.portal.Desktop";
  public const string PortalObjectPath = "/org/freedesktop/portal/desktop";

  private static readonly TimeSpan DefaultUserInteractionTimeout = TimeSpan.FromSeconds(120);

  private readonly ILogger _logger = logger;
  private readonly Lock _sync = new();

  private Connection? _connection;
  private ConnectionInfo? _connectionInfo;
  private int _tokenCounter;

  public bool IsConnected => _connection is not null;

  private Connection Connection
  {
    get
    {
      lock (_sync)
      {
        return _connection ?? throw new InvalidOperationException("The portal connection is not established.");
      }
    }
  }

  public async Task ConnectAsync(CancellationToken cancellationToken = default)
  {
    lock (_sync)
    {
      if (_connection is not null)
      {
        return;
      }
    }

    var connection = new Connection(Address.Session);
    var connectionInfo = await connection.ConnectAsync().WaitAsync(cancellationToken);

    lock (_sync)
    {
      if (_connection is not null)
      {
        connection.Dispose();
        return;
      }

      _connection = connection;
      _connectionInfo = connectionInfo;
    }
  }

  public T CreateProxy<T>(string objectPath)
    where T : IDBusObject
  {
    lock (_sync)
    {
      return Connection.CreateProxy<T>(PortalBusName, new ObjectPath(objectPath));
    }
  }

  public void Dispose()
  {
    Connection? connection;

    lock (_sync)
    {
      connection = _connection;
      _connection = null;
      _connectionInfo = null;
    }

    connection?.Dispose();
  }

  /// <summary>Predicts the request object path the portal will emit for the given handle token.</summary>
  public string GetExpectedRequestPath(string handleToken)
  {
    var localName = GetConnectionInfo().LocalName;
    var senderName = localName.TrimStart(':').Replace('.', '_');
    return $"{PortalObjectPath}/request/{senderName}/{handleToken}";
  }

  /// <summary>Generates a handle token valid for portal object paths ([A-Za-z0-9_]).</summary>
  public string NextHandleToken()
  {
    var number = Interlocked.Increment(ref _tokenCounter);
    return $"bitboundcu{number}";
  }

  /// <summary>
  /// Subscribes to the Response signal for a freshly predicted request path, invokes
  /// <paramref name="trigger"/>, and awaits (response, results).
  /// </summary>
  public async Task<(uint Response, IDictionary<string, object> Results)> RequestAsync(
    string handleToken,
    Func<Task> trigger,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default)
  {
    var expectedRequestPath = GetExpectedRequestPath(handleToken);
    var connection = Connection;

    var requestProxy = connection.CreateProxy<IXdgRequest>(PortalBusName, new ObjectPath(expectedRequestPath));

    var completion = new TaskCompletionSource<(uint, IDictionary<string, object>)>(TaskCreationOptions.RunContinuationsAsynchronously);

    using var signalSubscription = await requestProxy.WatchResponseAsync(
      data =>
      {
        _logger.LogDebug("Portal Response signal for {Path}: code={Code}", expectedRequestPath, data.response);
        completion.TrySetResult((data.response, data.results));
      },
      exception =>
      {
        _logger.LogError(exception, "Error on Response signal for {Path}", expectedRequestPath);
        completion.TrySetException(exception);
      });

    await trigger();

    using var timeoutSource = new CancellationTokenSource(timeout ?? DefaultUserInteractionTimeout);
    using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

    try
    {
      return await completion.Task.WaitAsync(linkedSource.Token);
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
      throw new TimeoutException($"Timed out waiting for a portal response at {expectedRequestPath}. The user may not have answered the permission prompt.");
    }
  }

  private ConnectionInfo GetConnectionInfo()
  {
    lock (_sync)
    {
      return _connectionInfo ?? throw new InvalidOperationException("The portal connection is not established.");
    }
  }
}
