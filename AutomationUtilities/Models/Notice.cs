using System.ComponentModel.DataAnnotations;

namespace AutomationUtilities.Models
{
    public class Notice
    {
        public int Id { get; set; }

        [StringLength(63)]
        public string? Subject { get; set; }

        [StringLength(512), UIHint("Message")]
        //[DataType(DataType.MultilineText)]
        public string? Message { get; set; }

        [Display(Name = "Item Path")]
        public string? ItemPath { get; set; }

        [Display(Name = "Scheduled Time")]
        public DateTime ScheduledTime { get; set; }

        public bool Sent { get; set; }

        [ScaffoldColumn(false)]
        public DateTime LastUpdated { get; set; }

        public Notice()
        {
        }

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
