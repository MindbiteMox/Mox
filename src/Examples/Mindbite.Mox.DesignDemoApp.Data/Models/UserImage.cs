using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Mindbite.Mox.DesignDemoApp.Data.Models
{
    [Table(nameof(UserImage), Schema = "Designs")]
    public class UserImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public string Filename { get; set; }

        [NotMapped]
        public string FilePath => $"media/userimages/{this.UserId}/{this.Filename}";
        [NotMapped]
        public string FileUrl => $"/static/media/userimages/{this.UserId}/{this.Filename}";
    }
}
