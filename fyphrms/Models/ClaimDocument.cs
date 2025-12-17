// ClaimDocument.cs 
using System.ComponentModel.DataAnnotations;

namespace fyphrms.Models
{
    public class ClaimDocument
    {
        [Key]
        public int DocumentID { get; set; }

        [Required]
        public int ClaimID { get; set; }
        public EClaim Claim { get; set; } = default!;

        [Required]
        public string FilePath { get; set; } = string.Empty; 

        public string? FileName { get; set; }
    }
}