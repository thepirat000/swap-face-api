using swap_faces;
using NUnit.Framework;
using System.Threading.Tasks;
using swap_faces.Swap;
using swap_faces.Helpers;
using swap_faces.Dto;
using System.Collections.Generic;
using System;
using System.Text.Json;
using System.Diagnostics;

namespace swap_faces_test
{
    public class SwapFaceTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test()
        {
            IFfMpegHelper ff = new FfMpegHelper(new ShellHelper());
            var dur = ff.GetVideoDuration(@"d:\video\lisztr2day1.mp4");
            var dur0 = ff.GetVideoDuration(@"d:\video\lisztr2day1asdfadf.mp4");
        }

        [Test]
        public async Task Test_Integration_YTVideo_ImageUrl_FrameAt_Trim_SingleFace()
        {
            var shellHelper = new ShellHelper();
            var swap = new SwapFaceProcessor(new ImageDownloader(), new YoutubeHelper(shellHelper), new FfMpegHelper(shellHelper), shellHelper);
            var request = new SwapFacesRequest()
            {
                SuperResolution = true,
                TargetMedia = new TargetMedia()
                {
                    MediaType = MediaType.Video,
                    SourceType = TargetMediaSourceType.Url,
                    Id = "https://www.youtube.com/watch?v=NMvMR-jNSKg",
                    StartAtTime = "00:00:07",
                    EndAtTime = "00:00:27"
                },
                SwapFaces = new List<SwapFace>()
                {
                    new SwapFace()
                    {
                        SourceType = FaceFromType.ImageUrl,
                        SourceId = "https://i.imgur.com/NMVdnei.jpeg",
                        TargetType = FaceFromType.FrameAtTarget,
                        TargetId = "00:00:15.500"
                    }
                }
            };
            var result = await swap.Process(request, null);


            Console.WriteLine("REQUEST: ");
            Console.WriteLine(JsonSerializer.Serialize(request, new JsonSerializerOptions() { WriteIndented = true }));
            Console.WriteLine("");
            Console.WriteLine("RESULT: ");
            Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions() { WriteIndented = true }));
        }

        [Test]
        public async Task Test_ShellHelper_()
        {
            IShellHelper shellHelper = new ShellHelper();
            var cmds = new string[] 
            { 
                "dir",
                "ping -n 10 127.0.0.1 >NUL",
                "echo fede"
            };
            var res = await shellHelper.ExecuteWithTimeout(cmds, null, 1, err =>
            {
                Debug.WriteLine("ERR: " + err);
            }, 
            txt =>
            {
                Debug.WriteLine("INFO: " + txt);
            });

            Assert.AreEqual(0, res.ExitCode);
        }
    }
}