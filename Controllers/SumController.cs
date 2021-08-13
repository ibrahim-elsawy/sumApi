using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using sumApi.Controllers.Models.DTOs.Requests;
using System.Text;
using System.Net.Http;
using System.Net.Http.Json;

namespace sumApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [AllowAnonymous]
    public class SumController : ControllerBase
    {
        [HttpPost]
        [Route("file")]
        public async Task<IActionResult> SummurizeFile([FromForm] FileRequest fileRequest)
        {
            try
            {
                if(fileRequest.file.Length < 1000000)
				{
					//call flask server
					var text = ReadFile(fileRequest.file);
					var sum = await HandleRequest(text);
					return Ok($"{ sum }");

				}
				else
                {
                    return BadRequest();
                }
            }catch (Exception){
                return BadRequest();
            }
		}
        [HttpPost]
        [Route("text")]
        public async Task<IActionResult> SummurizeText([FromForm] string text)
        {
            try
            {
                if( Encoding.UTF8.GetByteCount(text) < 1000000)
                {
                    //call flask server
                    var sum = await HandleRequest(text);
                    return Ok($"{sum}");

                }
                else
                {
                    return BadRequest();
                }
            }catch (Exception){
                return BadRequest();
            }
		}

        public async Task<string> HandleRequest(string text)
        {
			var payload = new ProcessPayload { data = text };
			HttpClient client = new HttpClient();
			// Call asynchronous network methods in a try/catch block to handle exceptions. 
			try	
             { 
                 HttpResponseMessage response = await client.PostAsJsonAsync("http://localhost:5000/process", payload); 
                 response.EnsureSuccessStatusCode(); 
                 string responseBody = await response.Content.ReadAsStringAsync(); 
                 Console.WriteLine(responseBody); 
                 return responseBody;
                 } catch(HttpRequestException e) { 
                     Console.WriteLine("\nException Caught!");	
                     Console.WriteLine("Message :{0} ",e.Message); 
                     return e.Message.ToString();
                }
        }

        public string ReadFile(IFormFile file)
        {
            var result = new StringBuilder(); 
            using (var reader = new StreamReader(file.OpenReadStream())) 
            {
				while (reader.Peek() >= 0) 
                {
					result.AppendLine(reader.ReadLine());
				}
			} 
            return result.ToString(); 
            // using (var ms = new MemoryStream())
            // {
			// 	await file.CopyToAsync(ms);
			// 	return Convert.ToString(ms);
			// }
		}
    }
}