using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using sumApi.Configuration;
using sumApi.Data;

namespace sumApi
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.Configure<JwtConfig>(Configuration.GetSection("JwtConfig"));
			services.AddDbContextPool<ApiDbContext>(
			    options => options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));
			// services.AddDbContextPool<ApiDbContext>( 
			//     options => options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
			// "DefaultConnection" : "server=db;database=TestDB;Trusted_Connection=false;user=SA;password=Sawy4507@"
			var key = Encoding.ASCII.GetBytes(Configuration["JwtConfig:Secret"]);
			var tokenValidationParams = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ValidateIssuer = false,
				ValidateAudience = false,
				ValidateLifetime = true,
				RequireExpirationTime = false,
				ClockSkew = TimeSpan.Zero
			};
			services.AddSingleton(tokenValidationParams);
			services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(jwt =>
			{

				jwt.SaveToken = true;
				jwt.TokenValidationParameters = tokenValidationParams;
			});

			services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
				    .AddEntityFrameworkStores<ApiDbContext>();

			services.AddControllers(options =>
			{
				options.CacheProfiles.Add("1minutes", new CacheProfile
				{
					Duration = 1 * 60,
					Location = ResponseCacheLocation.Any,
			    // VaryByHeader = "Accept-Language"
		    });
			}
			);
			services.AddResponseCaching(options =>
			{
				options.MaximumBodySize = 1024;
			});
			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "sumApi", Version = "v1" });
			});
			
			services.AddCors(options =>
			{
				options.AddPolicy("Open", builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
			});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				System.Console.WriteLine("this dev .............");
				app.UseDeveloperExceptionPage();
				app.UseSwagger();
				app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "sumApi v1"));
			}
			else
			{
				app.UseStatusCodePagesWithReExecute("/error/{0}");
			}

			// app.UseHttpsRedirection();

			app.UseRouting();

			app.UseAuthorization();
			app.UseCors("Open");

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});

		}
	}
}
