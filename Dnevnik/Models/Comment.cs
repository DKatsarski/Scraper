namespace Dnevnik.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public int CommentNumber { get; set; }
        public string? Author  { get; set; }
        public string? AuthorsInfo { get; set; }
        public int AuthorsRating { get; set; }
        public DateTime? DatePosted { get; set; }
        public string? Tone { get; set; }
        public string? CommentLink { get; set; }
        public int NegativeReactions { get; set; }
        public int PositiveReactions { get; set; }
        public string? ArticleTitle { get; set; }
        public int ArticleId { get; set; }
        public Article Article { get; set; }
    }
}
