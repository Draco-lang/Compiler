namespace Draco.Editor.Web;

internal static class Helpers
{

    public static string Base64ToBase64URL(this string str) =>
        str.TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    public static string Base64URLToBase64(this string strBuffer) =>
        Uri.UnescapeDataString(strBuffer)
            .Replace('_', '/')
            .Replace('-', '+')
            .PadRight(4 * ((strBuffer.Length + 3) / 4), '=');
}
