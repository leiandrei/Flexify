using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymCrudApp.Models
{
    public class Attendance : IValidatableObject
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int attendanceId { get; set; }

        [Required(ErrorMessage = "This field is required. Please select a member.")]
        public int memberId { get; set; }

        [ForeignKey("memberId")]
        public Members? Member { get; set; }

        [Required(ErrorMessage = "This field is required. Please provide a check-in time.")]
        [DataType(DataType.DateTime)]
        public DateTime checkIn { get; set; } = DateTime.Now;

        // Nullable: stays null when the member checks in, updated later when they leave
        [DataType(DataType.DateTime)]
        public DateTime? checkOut { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (checkOut.HasValue && checkOut.Value <= checkIn)
            {
                yield return new ValidationResult(
                    "Check-out time must be later than the check-in time.",
                    new[] { nameof(checkOut) });
            }
        }
    }
}