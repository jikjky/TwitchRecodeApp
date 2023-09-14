using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static TwitchRecodeApp.Class.TwitchApi;

namespace TwitchRecodeApp.Class
{
    public class YoutubeApi
    {
        OSPlatform oSPlatform;
        public YoutubeApi(OSPlatform _oSPlatform) 
        {
            oSPlatform = _oSPlatform;
        }

        public async void Upload(UserInfo userInfo, Action<string> writeLog)
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
            var a = youtubeService.PlaylistItems;

            new Thread(() =>
            {
                FileInfo fileInfo = new FileInfo(userInfo.FilePath);
                string name = Path.GetFileNameWithoutExtension(fileInfo.Name);
                name = name.Replace("_", "");
                // JSON 객체 생성
                var data = new
                {
                    title = name,
                    playlist_title = "jikjky",
                    description = name
                };


                // JSON 문자열로 직렬화
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);

                // JSON 파일에 저장
                System.IO.File.WriteAllText($"{userInfo.LoginId}.json", json);


                string cmd = $"Python youtube_uploader_selenium/upload.py --meta {userInfo.LoginId}.json --video {userInfo.FilePath}";
                writeLog.Invoke($"{cmd}");
                ProcessStartInfo? psi = null;
                // ProcessStartInfo 설정
                if (oSPlatform == OSPlatform.Windows)
                {
                    psi = new ProcessStartInfo("powershell.exe")
                    {
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                }
                else if (oSPlatform == OSPlatform.Linux)
                {
                    psi = new ProcessStartInfo("/bin/bash")
                    {
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                }

                if (psi != null)
                {
                    // 프로세스 시작
                    Process process = new Process { StartInfo = psi };
                    process.Start();
                    // streamlink 명령어를 명령 프롬프트에 입력
                    process.StandardInput.WriteLine(cmd);
                    process.StandardInput.Flush();
                    process.StandardInput.Close();

                    while (true)
                    {
                        if (process.HasExited)
                        {
                            break;
                        }
                        writeLog.Invoke($"now {userInfo.LoginId} Uploading");
                        Thread.Sleep(5000);
                    }
                    process.WaitForExit();

                    // 출력 내용 확인
                    writeLog.Invoke($"{userInfo.LoginId} Upload End");

                    // 프로세스 종료
                    process.Close();
                    userInfo.IsUploadComplete = true;
                    System.IO.File.Delete($"{userInfo.LoginId}.json");
                }
            })
            { IsBackground = true }.Start();
        }
    }
}
