// users tbl: id, email, password_hash, role, is_active

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymCrudApp.Models
{
    public class Users
    {
        // for primary key
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int userId { get; set; }
        
        // user email
        [Required(ErrorMessage = "This field is required. Please provide a valid email.")]
        [EmailAddress(ErrorMessage = "Invalid email address. Please provide a valid email.")]
        [StringLength(100, MinimumLength = 10, ErrorMessage = "The field email must be between 10 and 100 characters long.")]
        [GmailOnly]
        public string userEmail { get; set; } = string.Empty;
        
        // passwords
        [Required(ErrorMessage = "This field is required. Please provide a valid password.")]
        [StringLength(255, MinimumLength = 8, ErrorMessage = "The field password must be between 8 and 255 characters long.")]
        public string passwordHash { get; set; } = string.Empty;
        
        // roles: standard as members
        [Required(ErrorMessage = "This field is required. Select a role.")]
        [RegularExpression("^(Admin|Member)$", ErrorMessage = "Role must be Admin or Member")] // idk if kasama dapat ang staff
        public string role {  get; set; } = "Member";
        
        public bool isActive { get; set; } = true;
    }
}