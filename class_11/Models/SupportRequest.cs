namespace class_11.Models
{
    public class SupportRequest
    {
        public int Id { get; set; }
        public string UserName { get; set; } = "";
        public string Topic { get; set; } = "";
        public string Description { get; set; } = "";
        public string Priority { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}