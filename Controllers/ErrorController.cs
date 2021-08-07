using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace sumApi.Controllers
{
	[ApiController]
	[Route("error/{statusCode}")]
	public class ErrorController : ControllerBase
	{
		[HttpGet()]
		public async Task<IActionResult> HttpStatusCodeHandler(int statusCode)
		{
			await Task.Run(() =>
			{

				switch (statusCode)
				{
					case 404:
						Console.WriteLine($"error of {statusCode}");
						break;
				}

			});
			return NotFound();
		}
	}
}