// Licensed under the MIT License.
// See the LICENSE file in the project root for full license information.

using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mailgo.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(Mailgo.Api.Data.ApplicationDbContext))]
    [Migration("20251211000000_RenameTablesToSingular")]
    public partial class RenameTablesToSingular : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                migrationBuilder.CreateTable(
                    name: "Campaign",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "TEXT", nullable: false),
                        Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                        Subject = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                        FromName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                        FromEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                        HtmlBody = table.Column<string>(type: "TEXT", nullable: false),
                        TextBody = table.Column<string>(type: "TEXT", nullable: true),
                        Status = table.Column<int>(type: "INTEGER", nullable: false),
                        CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                        LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                        TargetRecipientCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Campaign", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "Recipient",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "TEXT", nullable: false),
                        Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                        FirstName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                        LastName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                        CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Recipient", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "CampaignSendLog",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "TEXT", nullable: false),
                        CampaignId = table.Column<Guid>(type: "TEXT", nullable: false),
                        RecipientId = table.Column<Guid>(type: "TEXT", nullable: false),
                        Status = table.Column<int>(type: "INTEGER", nullable: false),
                        ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                        SentAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_CampaignSendLog", x => x.Id);
                        table.ForeignKey(
                            name: "FK_CampaignSendLog_Campaign_CampaignId",
                            column: x => x.CampaignId,
                            principalTable: "Campaign",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                        table.ForeignKey(
                            name: "FK_CampaignSendLog_Recipient_RecipientId",
                            column: x => x.RecipientId,
                            principalTable: "Recipient",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateIndex(
                    name: "IX_CampaignSendLog_CampaignId_RecipientId",
                    table: "CampaignSendLog",
                    columns: new[] { "CampaignId", "RecipientId" });

                migrationBuilder.CreateIndex(
                    name: "IX_CampaignSendLog_RecipientId",
                    table: "CampaignSendLog",
                    column: "RecipientId");

                migrationBuilder.CreateIndex(
                    name: "IX_Recipient_Email",
                    table: "Recipient",
                    column: "Email",
                    unique: true);

                migrationBuilder.Sql(
                    """
                    INSERT INTO "Campaign" ("Id", "Name", "Subject", "FromName", "FromEmail", "HtmlBody", "TextBody", "Status", "CreatedAt", "LastUpdatedAt", "TargetRecipientCount")
                    SELECT "Id", "Name", "Subject", "FromName", "FromEmail", "HtmlBody", "TextBody", "Status", "CreatedAt", "LastUpdatedAt", "TargetRecipientCount" FROM "Campaigns";
                    """);

                migrationBuilder.Sql(
                    """
                    INSERT INTO "Recipient" ("Id", "Email", "FirstName", "LastName", "CreatedAt")
                    SELECT "Id", "Email", "FirstName", "LastName", "CreatedAt" FROM "Recipients";
                    """);

                migrationBuilder.Sql(
                    """
                    INSERT INTO "CampaignSendLog" ("Id", "CampaignId", "RecipientId", "Status", "ErrorMessage", "SentAt")
                    SELECT "Id", "CampaignId", "RecipientId", "Status", "ErrorMessage", "SentAt" FROM "CampaignSendLogs";
                    """);

                migrationBuilder.DropTable(
                    name: "CampaignSendLogs");

                migrationBuilder.DropTable(
                    name: "Campaigns");

                migrationBuilder.DropTable(
                    name: "Recipients");
            }
            else
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_CampaignSendLogs_Campaigns_CampaignId",
                    table: "CampaignSendLogs");

                migrationBuilder.DropForeignKey(
                    name: "FK_CampaignSendLogs_Recipients_RecipientId",
                    table: "CampaignSendLogs");

                migrationBuilder.DropPrimaryKey(
                    name: "PK_Recipients",
                    table: "Recipients");

                migrationBuilder.DropPrimaryKey(
                    name: "PK_Campaigns",
                    table: "Campaigns");

                migrationBuilder.DropPrimaryKey(
                    name: "PK_CampaignSendLogs",
                    table: "CampaignSendLogs");

                migrationBuilder.RenameTable(
                    name: "Recipients",
                    newName: "Recipient");

                migrationBuilder.RenameTable(
                    name: "Campaigns",
                    newName: "Campaign");

                migrationBuilder.RenameTable(
                    name: "CampaignSendLogs",
                    newName: "CampaignSendLog");

                migrationBuilder.RenameIndex(
                    name: "IX_Recipients_Email",
                    table: "Recipient",
                    newName: "IX_Recipient_Email");

                migrationBuilder.RenameIndex(
                    name: "IX_CampaignSendLogs_RecipientId",
                    table: "CampaignSendLog",
                    newName: "IX_CampaignSendLog_RecipientId");

                migrationBuilder.RenameIndex(
                    name: "IX_CampaignSendLogs_CampaignId_RecipientId",
                    table: "CampaignSendLog",
                    newName: "IX_CampaignSendLog_CampaignId_RecipientId");

                migrationBuilder.AddPrimaryKey(
                    name: "PK_Recipient",
                    table: "Recipient",
                    column: "Id");

                migrationBuilder.AddPrimaryKey(
                    name: "PK_Campaign",
                    table: "Campaign",
                    column: "Id");

                migrationBuilder.AddPrimaryKey(
                    name: "PK_CampaignSendLog",
                    table: "CampaignSendLog",
                    column: "Id");

                migrationBuilder.AddForeignKey(
                    name: "FK_CampaignSendLog_Campaign_CampaignId",
                    table: "CampaignSendLog",
                    column: "CampaignId",
                    principalTable: "Campaign",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                    name: "FK_CampaignSendLog_Recipient_RecipientId",
                    table: "CampaignSendLog",
                    column: "RecipientId",
                    principalTable: "Recipient",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                migrationBuilder.CreateTable(
                    name: "Campaigns",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "TEXT", nullable: false),
                        Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                        Subject = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                        FromName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                        FromEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                        HtmlBody = table.Column<string>(type: "TEXT", nullable: false),
                        TextBody = table.Column<string>(type: "TEXT", nullable: true),
                        Status = table.Column<int>(type: "INTEGER", nullable: false),
                        CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                        LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                        TargetRecipientCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Campaigns", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "Recipients",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "TEXT", nullable: false),
                        Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                        FirstName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                        LastName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                        CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_Recipients", x => x.Id);
                    });

                migrationBuilder.CreateTable(
                    name: "CampaignSendLogs",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(type: "TEXT", nullable: false),
                        CampaignId = table.Column<Guid>(type: "TEXT", nullable: false),
                        RecipientId = table.Column<Guid>(type: "TEXT", nullable: false),
                        Status = table.Column<int>(type: "INTEGER", nullable: false),
                        ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                        SentAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                    },
                    constraints: table =>
                    {
                        table.PrimaryKey("PK_CampaignSendLogs", x => x.Id);
                        table.ForeignKey(
                            name: "FK_CampaignSendLogs_Campaigns_CampaignId",
                            column: x => x.CampaignId,
                            principalTable: "Campaigns",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                        table.ForeignKey(
                            name: "FK_CampaignSendLogs_Recipients_RecipientId",
                            column: x => x.RecipientId,
                            principalTable: "Recipients",
                            principalColumn: "Id",
                            onDelete: ReferentialAction.Cascade);
                    });

                migrationBuilder.CreateIndex(
                    name: "IX_CampaignSendLogs_CampaignId_RecipientId",
                    table: "CampaignSendLogs",
                    columns: new[] { "CampaignId", "RecipientId" });

                migrationBuilder.CreateIndex(
                    name: "IX_CampaignSendLogs_RecipientId",
                    table: "CampaignSendLogs",
                    column: "RecipientId");

                migrationBuilder.CreateIndex(
                    name: "IX_Recipients_Email",
                    table: "Recipients",
                    column: "Email",
                    unique: true);

                migrationBuilder.Sql(
                    """
                    INSERT INTO "Campaigns" ("Id", "Name", "Subject", "FromName", "FromEmail", "HtmlBody", "TextBody", "Status", "CreatedAt", "LastUpdatedAt", "TargetRecipientCount")
                    SELECT "Id", "Name", "Subject", "FromName", "FromEmail", "HtmlBody", "TextBody", "Status", "CreatedAt", "LastUpdatedAt", "TargetRecipientCount" FROM "Campaign";
                    """);

                migrationBuilder.Sql(
                    """
                    INSERT INTO "Recipients" ("Id", "Email", "FirstName", "LastName", "CreatedAt")
                    SELECT "Id", "Email", "FirstName", "LastName", "CreatedAt" FROM "Recipient";
                    """);

                migrationBuilder.Sql(
                    """
                    INSERT INTO "CampaignSendLogs" ("Id", "CampaignId", "RecipientId", "Status", "ErrorMessage", "SentAt")
                    SELECT "Id", "CampaignId", "RecipientId", "Status", "ErrorMessage", "SentAt" FROM "CampaignSendLog";
                    """);

                migrationBuilder.DropTable(
                    name: "CampaignSendLog");

                migrationBuilder.DropTable(
                    name: "Campaign");

                migrationBuilder.DropTable(
                    name: "Recipient");
            }
            else
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_CampaignSendLog_Campaign_CampaignId",
                    table: "CampaignSendLog");

                migrationBuilder.DropForeignKey(
                    name: "FK_CampaignSendLog_Recipient_RecipientId",
                    table: "CampaignSendLog");

                migrationBuilder.DropPrimaryKey(
                    name: "PK_Recipient",
                    table: "Recipient");

                migrationBuilder.DropPrimaryKey(
                    name: "PK_CampaignSendLog",
                    table: "CampaignSendLog");

                migrationBuilder.DropPrimaryKey(
                    name: "PK_Campaign",
                    table: "Campaign");

                migrationBuilder.RenameTable(
                    name: "Recipient",
                    newName: "Recipients");

                migrationBuilder.RenameTable(
                    name: "CampaignSendLog",
                    newName: "CampaignSendLogs");

                migrationBuilder.RenameTable(
                    name: "Campaign",
                    newName: "Campaigns");

                migrationBuilder.RenameIndex(
                    name: "IX_Recipient_Email",
                    table: "Recipients",
                    newName: "IX_Recipients_Email");

                migrationBuilder.RenameIndex(
                    name: "IX_CampaignSendLog_RecipientId",
                    table: "CampaignSendLogs",
                    newName: "IX_CampaignSendLogs_RecipientId");

                migrationBuilder.RenameIndex(
                    name: "IX_CampaignSendLog_CampaignId_RecipientId",
                    table: "CampaignSendLogs",
                    newName: "IX_CampaignSendLogs_CampaignId_RecipientId");

                migrationBuilder.AddPrimaryKey(
                    name: "PK_Recipients",
                    table: "Recipients",
                    column: "Id");

                migrationBuilder.AddPrimaryKey(
                    name: "PK_CampaignSendLogs",
                    table: "CampaignSendLogs",
                    column: "Id");

                migrationBuilder.AddPrimaryKey(
                    name: "PK_Campaigns",
                    table: "Campaigns",
                    column: "Id");

                migrationBuilder.AddForeignKey(
                    name: "FK_CampaignSendLogs_Campaigns_CampaignId",
                    table: "CampaignSendLogs",
                    column: "CampaignId",
                    principalTable: "Campaigns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);

                migrationBuilder.AddForeignKey(
                    name: "FK_CampaignSendLogs_Recipients_RecipientId",
                    table: "CampaignSendLogs",
                    column: "RecipientId",
                    principalTable: "Recipients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            }
        }
    }
}
