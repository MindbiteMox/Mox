using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Mindbite.Mox.DesignDemoApp.Data.Models
{
    [Table(nameof(Image), Schema = "Designs")]
    public class Image
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public Guid UID { get; set; }
        public int DesignId { get; set; }
        [Display(Name = "Bildtitel")]
        public string Title { get; set; }

        public bool IsDeleted { get; set; }

        public Design Design { get; set; }


        [NotMapped]
        public string RelativeDirPath => $"media/designs/{DesignId}";
        [NotMapped]
        public string RelativePath => $"{this.RelativeDirPath}/{UID}.jpg";
    }
}
