using Confluent.Kafka;
using Newtonsoft.Json;
using Spaghetti.Common.Kafka;
using Spaghetti.Common.Models.Supabase;
using Spaghetti.IngestService.DTO;

namespace Spaghetti.IngestService.Consumers
{
    public class MangaDetailConsumer : TopicConsumer<Null, ICollection<MangaInfoGeneric>>
    {
        private readonly Supabase.Client _supasbaseClient;
        public MangaDetailConsumer(ILogger<MangaDetailConsumer> logger, Supabase.Client supasbaseClient) : base(logger, "Manga-Detail")
        {
            _supasbaseClient = supasbaseClient;
        }
        protected override async Task OnMessageAsync(ConsumeResult<Null, ICollection<MangaInfoGeneric>> consumeResult, CancellationToken stoppingToken)
        {
            var message = consumeResult.Message.Value;
            try
            {
                HashSet<string> duplicate = new();
                List<MangaInfoGeneric> mangaInfoGenerics = new();

                DateTime now = DateTime.Now;
                Console.WriteLine("Processing at: " + now);
                foreach (var item in message)
                    if (duplicate.Add(item.Id))
                    {
                        mangaInfoGenerics.Add(new MangaInfoGeneric()
                        {
                            Id = item.Id,
                            Title = item.Title,
                            CreatedAt = item.CreatedAt,
                            UpdatedAt = now,
                            Thumbnail = item.Thumbnail,
                            Author = item.Author,
                            CurrentChapter = item.CurrentChapter,
                            Genre = item.Genre,
                            ListChapter = item.ListChapter,
                            Processed = true
                        });
                    }

                _ = await _supasbaseClient.From<MangaInfoGeneric>().Upsert(mangaInfoGenerics);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error When Trying to Upsert: {JsonConvert.SerializeObject(message)}");
            }
        }
    }
}
