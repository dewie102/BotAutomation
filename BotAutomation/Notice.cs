namespace BotAutomation
{
    public class Notice
    {
        public int Id { get; set; }

        public string? Subject { get; set; }

        public string? Message { get; set; }

        public string? ItemPath { get; set; }

        public DateTime ScheduledTime { get; set; }

        public bool Sent { get; set; }

        public DateTime LastUpdated { get; set; }

        public Notice(int id, string? subject, string? message, string? itemPath, DateTime scheduledTime, bool sent, DateTime lastUpdated)
        {
            Id = id;
            Subject = subject;
            Message = message;
            ItemPath = itemPath;
            ScheduledTime = scheduledTime;
            Sent = sent;
            LastUpdated = lastUpdated;
        }
    }
}
