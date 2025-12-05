// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Mailgo.Api.Background;
using Mailgo.Api.Data;
using Mailgo.Api.Services;
using Microsoft.EntityFrameworkCore;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(dataDirectory);

        var connectionString = builder.Configuration.GetConnectionString("Default")
                                ?? $"Data Source={Path.Combine(dataDirectory, "app.db")}";

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        builder.Services.AddSingleton<ICampaignSendSessionStore, InMemoryCampaignSendSessionStore>();
        builder.Services.AddSingleton<IEmailSender, MailKitEmailSender>();
        builder.Services.AddHostedService<CampaignSenderService>();

        builder.Services
            .AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("default", policy =>
            {
                policy
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin();
            });
        });

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.Migrate();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors("default");

        app.MapControllers();

        app.Run();
    }
}