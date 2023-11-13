using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Mindbite.Mox.Identity.Data.Models
{
    public class MagicLinkToken
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        [Required]
        [PersonalData]
        public string RequestedByClientInfo { get; set; }
        [Required]
        public string UserId { get; set; }
        public bool Used { get; set; }
        public bool Invalidated { get; set; }
        public DateTime ValidUntil { get; set; }
        public string NormalizedShortCode { get; set; }

        public MoxUser User { get; set; }
    }
}
