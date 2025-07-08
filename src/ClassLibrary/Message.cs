namespace ClassLibrary
{
    public class Message
    {
        public string FileName { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }

        public Message(string filename, string content, DateTime timestamp)
        {
            FileName = filename;
            Content = content;
            Timestamp = timestamp;
        }
    }
}
