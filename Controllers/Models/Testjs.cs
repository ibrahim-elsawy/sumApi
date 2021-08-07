using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace sumApi.Controllers.Models
{
	public class Testjs
	{
		// public IFormFile Fruit {get; set;}
		[Required]
		public int Id { get; set; }
	}
}