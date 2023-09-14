using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api;
using static TwitchRecodeApp.Class.TwitchApi;

namespace TwitchRecodeApp.Class
{
    public class SettingIni : ClassToIni
    {
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string RecodingId { get; set; } = "";
        public string Proxy { get; set; } = "";

        public SettingIni(FileInfo fileInfo) : base(fileInfo)
        {
        }
    }

    public class TwitchApi
    {
        private static async Task<string> GetAccessTokken(string ClientId, string ClientSecret)
        {
            string tokenUrl = "https://id.twitch.tv/oauth2/token";
            string postData = $"client_id={ClientId}&client_secret={ClientSecret}&grant_type=client_credentials";

            using var httpClient = new HttpClient();
            var content = new StringContent(postData);
            content.Headers.Clear();
            content.Headers.Add("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");

            HttpResponseMessage response = await httpClient.PostAsync(tokenUrl, content);
            string responseContent = await response.Content.ReadAsStringAsync();

            // Parse the response content to extract the access token
            // Note: For proper JSON parsing, you should consider using a JSON library.
            string[] parameters = responseContent.Split(',');
            string[] value = parameters[0].Split(':');
            string accessToken = value[1].Replace("\"", "").Trim();

            return accessToken;
        }

        public class UserInfo
        {
            public string LoginId { get; set; } = "";
            public string FilePath { get; set; } = "";            
            public DateTime StreamTime { get; set; }
            public bool IsStream { get; set; }
            public bool IsRecording { get; set; }
            public bool IsUploadStart { get; set; }
            public bool IsUploadComplete { get; set; }
            public bool IsNeedProcess { get; set; }
            public bool IsNeedDelete { get; set; }


        }

        List<UserInfo> userInfos = new List<UserInfo>();

        public TwitchApi(OSPlatform oSPlatform)
        {
            YoutubeApi youtubeApi = new YoutubeApi(oSPlatform);
            TwitchAPI twitchAPI = new TwitchAPI();
            SettingIni setting = new SettingIni(new FileInfo("Setting.ini"));
            twitchAPI.Settings.ClientId = setting.ClientId;
            twitchAPI.Settings.AccessToken = GetAccessTokken(setting.ClientId, setting.ClientSecret).Result;
            twitchAPI.Settings.Secret = setting.ClientSecret;
            while (true)
            {
                setting.LoadINI();
                var idList = setting.RecodingId.Split(',').ToList();

                idList.RemoveAll(x => string.IsNullOrWhiteSpace(x));

                foreach (var item in userInfos)
                {
                    var user = idList.Find(x => x == item.LoginId);
                    if (string.IsNullOrEmpty(user))
                    {
                        if (!item.IsNeedDelete)
                        {
                            WriteLog($"{item.LoginId} need Delete");
                        }
                        item.IsNeedDelete = true;
                    }
                    else
                    {
                        if (item.IsNeedDelete)
                        {
                            WriteLog($"{item.LoginId} need Delete cancel");
                        }
                        item.IsNeedDelete = false;
                    }
                }



                var users = twitchAPI.Helix.Users.GetUsersAsync(logins: idList).Result;

                foreach (var user in users.Users)
                {
                    if (!userInfos.Select(x => x.LoginId).Contains(user.Login))
                    {
                        userInfos.Add(new UserInfo() { LoginId = user.Login });
                        WriteLog($"{user.Login} add");
                    }
                }

                var streams = twitchAPI.Helix.Streams.GetStreamsAsync(userLogins: users.Users.Select(x => x.Login).ToList()).Result;
                foreach (var user in userInfos)
                {
                    if (streams.Streams.Select(x => x.UserLogin).Contains(user.LoginId))
                    {
                        if (user.IsStream == false)
                        {

                            var stream = streams.Streams.ToList().Find(x => x.UserLogin == user.LoginId);
                            if (stream != null)
                            {
                                user.StreamTime = stream.StartedAt;
                                user.IsStream = true;

                                new Thread(() =>
                                {
                                    // Twitch 채널과 저장 경로 설정
                                    string channel = user.LoginId; // 실제 Twitch 채널 이름으로 대체
                                    string outputPath = $"D:\\temp\\{channel}_{user.StreamTime.ToString("yyyyMMdd_hhmmss")}.ts"; // 저장 경로 및 파일 이름으로 대체
                                    user.FilePath = outputPath;
                                    // streamlink 명령어 구성
                                    string streamlinkCmd = $"streamlink {(string.IsNullOrEmpty(setting.Proxy) ? "":$"--http-proxy \"socks5h://{setting.Proxy}\"")} --twitch-disable-hosting --twitch-disable-ads twitch.tv/{channel} best -o \"{outputPath}\"";
                                    WriteLog($"{streamlinkCmd}");
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
                                        process.StandardInput.WriteLine(streamlinkCmd);
                                        process.StandardInput.Flush();
                                        process.StandardInput.Close();

                                        user.IsRecording = true;

                                        while (true)
                                        {
                                            if (process.HasExited)
                                            {
                                                break;
                                            }
                                            WriteLog($"now {channel} Recording");
                                            Thread.Sleep(5000);
                                        }
                                        process.WaitForExit();

                                        // 출력 내용 확인
                                        WriteLog($"{channel} Record End");

                                        // 프로세스 종료
                                        process.Close();
                                    }
                                    user.IsRecording = false;
                                })
                                { IsBackground = true }.Start();
                            }

                        }
                    }
                    else
                    {
                        if (user.IsStream == true)
                        {
                            user.IsStream = false;
                            user.IsNeedProcess = true;
                        }
                    }
                }

                List<UserInfo> deleteInfos = new List<UserInfo>();

                foreach (var user in userInfos)
                {
                    if (user.IsNeedProcess && !user.IsRecording)
                    {
                        //if (user.IsUploadStart == false)
                        //{
                        //    user.IsUploadStart = true;
                        //    WriteLog($"{user.LoginId} Processing");
                        //    youtubeApi.Upload(user, WriteLog);
                        //}
                        //if (user.IsUploadComplete == true)
                        //{
                        //    user.IsUploadStart = false;
                        //    user.IsUploadComplete = false;
                        //    user.IsNeedProcess = false;
                        //    if (user.IsNeedDelete)
                        //    {
                        //        deleteInfos.Add(user);
                        //    }
                        //    WriteLog($"{user.LoginId} Process end");
                        //}

                        user.IsUploadStart = false;
                        user.IsUploadComplete = false;
                        user.IsNeedProcess = false;
                        if (user.IsNeedDelete)
                        {
                            deleteInfos.Add(user);
                        }
                        WriteLog($"{user.LoginId} Process end");

                    }
                }

                foreach (var item in deleteInfos)
                {
                    WriteLog($"{item.LoginId} Delete");
                    userInfos.Remove(item);
                }

                if (DateTime.Now - lastAlive > TimeSpan.FromSeconds(10))
                {
                    lastAlive = DateTime.Now;
                    if (bWriteLog == false)
                    {
                        WriteLog("Alive");
                    }
                    bWriteLog = false;
                }
                Thread.Sleep(1000);
            }
        }

        bool bWriteLog = false;
        DateTime lastAlive = DateTime.MinValue;

        public void WriteLog(string text)
        {
            bWriteLog = true;
            Console.WriteLine($"{DateTime.Now.ToString("yyyyMMdd_HH:mm:ss_fff")} {text}");
            lastAlive = DateTime.Now;
        }
    }
}
