using System.ComponentModel.DataAnnotations;

namespace gView.Server.Models.Manage
{
    public class ManageLoginModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }
    }
}
