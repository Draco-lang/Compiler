using System.Runtime.InteropServices.JavaScript;

namespace Draco.Editor.Web;

public static partial class Interop
{
    [JSImport("Interop.sendMessage", "worker.js")]
    public static partial Task SendMessage(string type, string message);

    [JSExport]
    public static async Task OnMessage(string type, string message)
    {
        var msgs = Messages;
        if (msgs is not null) await msgs.Invoke(type, message);
    }

    public static event Func<string, string, Task>? Messages;
}
