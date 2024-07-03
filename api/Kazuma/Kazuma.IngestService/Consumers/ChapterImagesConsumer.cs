﻿using Confluent.Kafka;
using Kazuma.Common.Kafka;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Kazuma.IngestService.Consumers
{
    public class ChapterImagesConsumer : TopicConsumer<string, ChapterImage>
    {
        public ChapterImagesConsumer(ILogger<ChapterImagesConsumer> logger) : base(logger, "Chapter-Images")
        {
        }

        protected override Task OnMessageAsync(ConsumeResult<string, ChapterImage> consumeResult, CancellationToken stoppingToken)
        {
            Console.WriteLine("Consuming");
            var message = consumeResult.Message.Value;
            if (message.FileName.Contains("?"))
            {
                message.FileName = message.FileName.Replace("?", "");
            }
            using (Image image = Image.Load(message.ImageByte))
            {
                string directoryPath = "C:\\Users\\PC\\OneDrive\\Desktop\\Project\\Kazuma\\Yolov5\\input\\" + message.MangaName;
                Directory.CreateDirectory(directoryPath);
                string imagePath = Path.Combine(directoryPath, $"{message.FileName}.jpg");
                image.Save(imagePath, new JpegEncoder());
            }
            return Task.CompletedTask;
        }
    }

    public class ChapterImage
    {
        public string MangaName { get; set; }

        public byte[] ImageByte { get; set; }

        public string FileName { get; set; }
    }

}

