using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebDoNgot.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string FullName { get; set; }

        public string? Address { get; set; }

        public string? Age { get; set; }
    }
}