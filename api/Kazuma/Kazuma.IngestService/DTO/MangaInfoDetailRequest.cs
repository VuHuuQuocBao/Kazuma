namespace Spaghetti.IngestService.DTO
{
    public class MangaInfoDetailRequest
    {
        public string Id { get; set; }
        public string Thumbnail { get; set; }
        public string Authors { get; set; }
        public string Genres { get; set; }
        public string ListChapter { get; set; }
        public string CurrentChapter { get; set; }
        public string Title { get; set; }
    }
}
