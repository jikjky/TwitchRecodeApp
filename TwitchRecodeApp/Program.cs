using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using System.Runtime.InteropServices;
using TwitchRecodeApp.Class;

internal class Program
{
    private static void Main(string[] args)
    {
        Task.Run(async () =>
        {
            UserCredential credential;
            using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    // This OAuth 2.0 access scope allows for full read/write access to the
                    // authenticated user's account.
                    new[] { YouTubeService.Scope.Youtube },
                    "user",
                    CancellationToken.None,
                    new FileDataStore("TwitchRecode")
                );
            }

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "TwitchRecode"
            });

            var a = youtubeService.Playlists.List("");
            var b = a.Execute();
            
        }).Wait();

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