using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using sumApi.Controllers.Models;

namespace sumApi.Data
{
	public class ApiDbContext : IdentityDbContext
	{
		// public virtual DbSet<Testjs> Items { get; set; }
		public virtual DbSet<RefreshToken> RefreshToken { get; set; }

		public ApiDbContext(DbContextOptions<ApiDbContext> options)
			: base(options)
		{

		}
	}
}