

using Backend.Extensions;

namespace Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var services = builder.Services;

            // Add services to the container.
            services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddServices();

            // Add CORS policy
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
            });

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            // Use the CORS policy
            app.UseCors("AllowAllOrigins");

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
