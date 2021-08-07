using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace sumApi.Controllers.Models.DTOs.Requests
{
    public class FileRequest
    {
        [Required]
        public IFormFile file { get; set; }
	}
}