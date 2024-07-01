using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Spaghetti.Common.Models.Supabase
{
    [Table("MangaInfoGeneric")]
    public class MangaInfoGeneric : BaseModel
    {
        [PrimaryKey("id")]
        public string Id { get; set; }
        [Column("title")]
        public string Title { get; set; }
        [Column("createdAt")]

        public DateTime CreatedAt { get; set; }
        [Column("updatedAt")]
        public DateTime UpdatedAt { get; set;}
        [Column("current_chapter")]
        public string CurrentChapter { get; set; }
        [Column("author")]
        public string Author { get; set; }
        [Column("genre")]
        public string Genre { get; set; }
        [Column("list_chapter")]
        public string ListChapter { get; set; }
        [Column("processed")]
        public bool Processed {  get; set; }
        [Column("thumbnail")]

        public string Thumbnail { get; set; }
    }
}
