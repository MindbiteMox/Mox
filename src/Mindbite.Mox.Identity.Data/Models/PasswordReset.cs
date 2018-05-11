using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Mindbite.Mox.Identity.Data.Models
{
    public class PasswordReset
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        [Required]
        public string ResetToken { get; set; }
        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        [Required]
        public string RequestedByClientInfo { get; set; }
        [Required]
        public DateTime CreatedOn { get; set; }
        [Required]
        public DateTime ValidUntil { get; set; }
        public bool Completed { get; set; }
        public DateTime? CompletedOn { get; set; }

        public MoxUser User { get; set; }
    }
}
