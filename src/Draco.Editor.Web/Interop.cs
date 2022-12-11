using System.Runtime.InteropServices.JavaScript;

namespace Draco.Editor.Web;

public static partial class Interop
{
    [JSImport("Interop.sendMessage", "worker.js")]
    public static partial void SendMessage(string type, string message);

    [JSExport]
    public static void OnMessage(string type, string message) => Messages?.Invoke(null, (type, message));

    public static event EventHandler<(string type, string message)>? Messages;
}
