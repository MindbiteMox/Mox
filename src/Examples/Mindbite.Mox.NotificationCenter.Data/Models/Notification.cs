using Microsoft.EntityFrameworkCore;
using Mindbite.Mox.Identity.Data.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mindbite.Mox.NotificationCenter.Data.Models
{
    public class Notification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string SubjectId { get; set; }
        public int? EntityId { get; set; }
        [ForeignKey("Reciever")]
        public string RecieverId { get; set; }
        public string ShortDescription { get; set; }
        public string URL { get; set; }
        public bool IsRead { get; set; }
        public DateTime SentTime { get; set; }

        public MoxUser Reciever { get; set; }

        public Notification() : this("") { }

        public Notification(string subjectId, int? entityId = null, string recieverId = null, string shortDescription = "", string url = "")
        {
            this.SubjectId = subjectId;
            this.EntityId = entityId;
            this.RecieverId = recieverId;
            this.ShortDescription = shortDescription;
            this.URL = url;
            this.IsRead = false;
            this.SentTime = DateTime.Now;
        }
    }

    public class NotificationMapping : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable(nameof(Notification), "MoxNotification");
            builder.HasOne(x => x.Reciever).WithMany().HasForeignKey(x => x.RecieverId);
        }
    }
}
