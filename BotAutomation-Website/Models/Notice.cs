using System.ComponentModel.DataAnnotations;

namespace BotAutomation_Website.Models
{
    public class Notice
    {
        public int Id { get; set; }

        [StringLength(63)]
        public string? Subject { get; set; }

        [StringLength(512)]
        public string? Message { get; set; }

        [Display(Name = "Item Path")]
        public string? ItemPath { get; set; }

        [Display(Name = "Scheduled Time")]
        public DateTime ScheduledTime { get; set; }

        public bool Sent { get; set; }

        [ScaffoldColumn(false)]
        public DateTime LastUpdated { get; set; }
    }
}
