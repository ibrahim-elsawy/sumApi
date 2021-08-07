using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using sumApi.Controllers.Models.DTOs.Requests;

namespace sumApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [AllowAnonymous]
    public class SumController : ControllerBase
    {
        [HttpPost]
        [Route("file")]
        public async Task<IActionResult> Summurize([FromBody] FileRequest fileRequest)
        {
            Console.WriteLine($"--------------------********************{fileRequest.file}--------------****************************");
            
			if(fileRequest.file.Length < 1000000)
            {
                //call flask server
                Console.WriteLine($"{fileRequest.file}");
				return Ok();

			}
            else
            {
				return BadRequest();
			}
		}
    }
}