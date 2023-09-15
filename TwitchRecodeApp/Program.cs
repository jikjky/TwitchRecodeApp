using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using System.Runtime.InteropServices;
using TwitchRecordApp.Class;

internal class Program
{
    private static void Main(string[] args)
    {
        OSPlatform oSPlatform = new OSPlatform();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            oSPlatform = OSPlatform.Windows;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            oSPlatform = OSPlatform.Linux;
        }

        TwitchApi twitchApi = new TwitchApi(oSPlatform);
    }
}