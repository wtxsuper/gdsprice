namespace QuantityCalculator
{
    internal class Message
    {
        public required string FileName { get; set; }
        public required string Content { get; set; }
        public required DateTime Timestamp { get; set; }
    }
}
