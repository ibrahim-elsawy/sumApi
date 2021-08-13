using System.ComponentModel.DataAnnotations;

namespace sumApi.Controllers.Models.DTOs.Requests
{
    public class ProcessPayload
    {
        [Required]
        public string data {get; set;}
    }
}