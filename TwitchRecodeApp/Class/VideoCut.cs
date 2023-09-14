using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using static TwitchRecodeApp.Class.TwitchApi;

namespace TwitchRecodeApp.Class
{
    public class VideoCut
    {
        public void Cut(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            string name = Path.GetFileNameWithoutExtension(fileInfo.Name);

            string inputVideoPath = filePath; // 입력 동영상 파일 경로
            string outputDirectory = "output"; // 자른 동영상을 저장할 디렉토리 경로
            TimeSpan segmentDuration = TimeSpan.FromHours(12); // 12시간 단위로 자르기

            // 출력 디렉토리가 없으면 생성
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // 입력 동영상 파일 미디어 정보 가져오기
            IMediaInfo mediaInfo = FFmpeg.GetMediaInfo(inputVideoPath).Result;

            // 총 동영상 길이
            TimeSpan totalDuration = mediaInfo.Duration;

            // 동영상을 12시간 단위로 자르기
            TimeSpan startTime = TimeSpan.Zero;
            int segmentNumber = 1;

            while (startTime < totalDuration)
            {
                TimeSpan endTime = startTime.Add(segmentDuration);
                if (endTime > totalDuration)
                {
                    endTime = totalDuration;
                }

                // 자를 동영상의 파일 이름 설정
                string outputVideoPath = Path.Combine(outputDirectory, $"{name}_{segmentNumber}{fileInfo.Extension}");

                // FFmpeg를 사용하여 동영상 자르기
                var conversion = FFmpeg.Conversions.New()
                    .AddParameter("-ss")
                    .AddParameter(startTime.ToString())
                    .AddParameter("-i")
                    .AddParameter(inputVideoPath)
                    .AddParameter("-to")
                    .AddParameter(endTime.ToString())
                    .SetOutput(outputVideoPath);

                conversion.Start().Wait();

                Console.WriteLine($"Segment {name}_{segmentNumber} completed.");

                // 다음 세그먼트 시작 시간 설정
                startTime = endTime;
                segmentNumber++;
            }

            Console.WriteLine("All segments created.");
        }
    }
}
