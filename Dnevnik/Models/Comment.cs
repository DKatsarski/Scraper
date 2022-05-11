namespace Dnevnik.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public string? Author  { get; set; }
        public Article Article { get; set; }
    }
}
