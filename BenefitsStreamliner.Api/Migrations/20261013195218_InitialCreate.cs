using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenefitsStreamliner.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SinEncrypted = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SinLast3 = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MailingAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Province = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CompanyAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompanyProvince = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    CompanyPostalCode = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Trade = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TrainingYear = table.Column<int>(type: "int", nullable: false),
                    TrainingFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrainingTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnedToWork = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayTo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CertificateNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TuitionCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EmployeeSignature = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmployeeSignDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RepName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RepPosition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RepSignature = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RepSignDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NextRecheckAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationRowId = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationDocuments_Applications_ApplicationRowId",
                        column: x => x.ApplicationRowId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDocuments_ApplicationRowId",
                table: "ApplicationDocuments",
                column: "ApplicationRowId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ApplicationId",
                table: "Applications",
                column: "ApplicationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_CertificateNumber_TuitionCode",
                table: "Applications",
                columns: new[] { "CertificateNumber", "TuitionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_ApplicationId",
                table: "Recommendations",
                column: "ApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationDocuments");

            migrationBuilder.DropTable(
                name: "Recommendations");

            migrationBuilder.DropTable(
                name: "Applications");
        }
    }
}
