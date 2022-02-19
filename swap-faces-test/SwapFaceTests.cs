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
        public async Task Test_Integration_YTVideo_ImageUrl_FrameAt_Trim_SingleFace()
        {
            var shellHelper = new ShellHelper();
            var swap = new SwapFaceProcessor(new ImageDownloader(), new YoutubeHelper(shellHelper), new FfMpegHelper(shellHelper), shellHelper);
            var request = new SwapFacesRequest()
            {
                TargetMedia = new TargetMedia()
                {
                    Type = TargetMedia.MediaType.VideoUrl,
                    Id = "https://www.youtube.com/watch?v=NMvMR-jNSKg",
                    StartAtTime = "00:00:04",
                    EndAtTime = "00:00:33"
                },
                SwapFaces = new List<SwapFace>()
                {
                    new SwapFace()
                    {
                        SourceType = SwapFace.FaceSourceType.ImageUrl,
                        SourceId = "https://i.imgur.com/NMVdnei.jpeg",
                        TargetType = SwapFace.FaceTargetType.FrameAt,
                        TargetId = "00:00:01.800"
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
    }
}