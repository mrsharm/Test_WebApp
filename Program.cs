using Azure.Storage.Blobs;

namespace WebApp_AppService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            var connectionString = app.Configuration.GetConnectionString("StorageAccount");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Storage account connection string is not configured.");
            }

            try
            {
                var blobServiceClient = new BlobServiceClient(connectionString);
                var accountInfo = blobServiceClient.GetAccountInfo();
                Console.WriteLine("Successfully connected to storage account.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to connect to storage account: {ex.Message}", ex);
            }

            app.Run();
        }
    }
}
