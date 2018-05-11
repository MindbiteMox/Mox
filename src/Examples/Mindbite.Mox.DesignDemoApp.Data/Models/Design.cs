using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mindbite.Mox.DesignDemoApp.Data.Models
{
    [Table(nameof(Design), Schema = "Designs")]
    public class Design
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        [Display(Name = "Titel")]
        public string Title { get; set; }
        [Display(Name = "Beskrivning")]
        public string Description { get; set; }

        public bool IsDeleted { get; set; }

        [Display(Name = "Bilder")]
        public ICollection<Image> Images { get; set; }
    }

    public class DesignMapping : IEntityTypeConfiguration<Design>, IEntityTypeConfiguration<Image>
    {
        public void Configure(EntityTypeBuilder<Design> builder)
        {
            builder.HasMany(x => x.Images)
                .WithOne(x => x.Design)
                .HasForeignKey(x => x.DesignId);
            builder.HasQueryFilter(x => x.IsDeleted == false);
        }

        public void Configure(EntityTypeBuilder<Image> builder)
        {
            builder.HasQueryFilter(x => x.IsDeleted == false);
        }
    }
}
