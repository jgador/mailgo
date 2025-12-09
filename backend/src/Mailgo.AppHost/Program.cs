// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text.Json.Serialization;
using Mailgo.Api.Background;
using Mailgo.Api.Data;
using Mailgo.Api.Health;
using Mailgo.Api.Options;
using Mailgo.Api.Services;
using Mailgo.Api.Stores;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(dataDirectory);

        var connectionString = builder.Configuration.GetConnectionString("Default")
                                ?? $"Data Source={Path.Combine(dataDirectory, "app.db")}";

        builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        builder.Services.Configure<SmtpEncryptionKeyOptions>(builder.Configuration.GetSection(SmtpEncryptionKeyOptions.SectionName));
        builder.Services.AddSingleton<ISmtpPasswordDecryptor, RsaSmtpPasswordDecryptor>();
        builder.Services.AddSingleton<ICampaignSendSessionStore, InMemoryCampaignSendSessionStore>();
        builder.Services.AddSingleton<IEmailSender, MailKitEmailSender>();
        builder.Services.AddScoped<CampaignStore>();
        builder.Services.AddScoped<RecipientStore>();
        builder.Services.AddHostedService<CampaignSenderService>();
        builder.Services
            .AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database");

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

        using var scope = app.Services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        using var dbContext = dbContextFactory.CreateDbContext();
        dbContext.Database.Migrate();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors("default");

        app.MapHealthChecks("/health");
        app.MapControllers();

        app.Run();
    }
}
