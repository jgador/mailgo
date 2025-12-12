// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using Mailgo.Api.Background;
using Mailgo.Api.Data;
using Mailgo.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Mailgo.AppHost.Tests;

public class MailgoApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection;

    public MailgoApiFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public TestEmailSender EmailSender => Services.GetRequiredService<TestEmailSender>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IDbContextFactory<ApplicationDbContext>));

            var hostedServiceDescriptor = services.SingleOrDefault(descriptor =>
                descriptor.ServiceType == typeof(IHostedService) &&
                descriptor.ImplementationType == typeof(CampaignSenderService));
            if (hostedServiceDescriptor is not null)
            {
                services.Remove(hostedServiceDescriptor);
            }

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<TestEmailSender>();
            services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<TestEmailSender>());

            services.AddDbContextFactory<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        using var dbContext = factory.CreateDbContext();
        await dbContext.Database.EnsureDeletedAsync().ConfigureAwait(false);
        await dbContext.Database.MigrateAsync().ConfigureAwait(false);
    }

    public async Task ExecuteDbContextAsync(Func<ApplicationDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        using var dbContext = factory.CreateDbContext();
        await action(dbContext).ConfigureAwait(false);
    }

    public async Task<T> ExecuteDbContextAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        using var dbContext = factory.CreateDbContext();
        return await action(dbContext).ConfigureAwait(false);
    }

    public Task InitializeAsync() => ResetDatabaseAsync();

    public new Task DisposeAsync()
    {
        _connection.Dispose();
        return base.DisposeAsync().AsTask();
    }
}

public class TestEmailSender : IEmailSender
{
    private readonly List<SendCall> _calls = new();

    public IReadOnlyList<SendCall> Calls => _calls;

    public void Reset() => _calls.Clear();

    public Task SendAsync(
        Domain.Entities.Campaign campaign,
        Domain.Entities.Recipient recipient,
        SmtpSettings settings,
        CancellationToken cancellationToken,
        string? overrideRecipientEmail = null)
    {
        _calls.Add(new SendCall(campaign, recipient, settings, overrideRecipientEmail));
        return Task.CompletedTask;
    }

    public record SendCall(
        Domain.Entities.Campaign Campaign,
        Domain.Entities.Recipient Recipient,
        SmtpSettings Settings,
        string? OverrideRecipientEmail);
}
