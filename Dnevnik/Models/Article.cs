using Dnevnik.Models;

namespace Dnevnik
{
    public class Article
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? Author { get; set; }
        public string? ArticleLink { get; set; }
        public string? Views { get; set; }
        public string? Keywords { get; set; }
        public DateTime? DatePublished { get; set; }
        public DateTime? DateModified { get; set; }
        public ICollection<Comment>? Comments { get; set; }
    }
}
