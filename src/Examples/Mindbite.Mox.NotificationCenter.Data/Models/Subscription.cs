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
    public class Subscription
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string SubjectId { get; set; }
        public int? EntityId { get; set; }
        [ForeignKey("Subscriber")]
        public string SubscriberId { get; set; }
        [ForeignKey("Sender")]
        public string SenderId { get; set; }

        public MoxUser Subscriber { get; set; }
        public MoxUser Sender { get; set; }

    }

    public class SubscriptionMapping : IEntityTypeConfiguration<Subscription>
    {
        public void Configure(EntityTypeBuilder<Subscription> builder)
        {
            builder.ToTable(nameof(Subscription), "MoxNotification");
            builder.HasOne(x => x.Subscriber).WithMany().HasForeignKey(x => x.SubscriberId);
            builder.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId);
        }
    }
}
