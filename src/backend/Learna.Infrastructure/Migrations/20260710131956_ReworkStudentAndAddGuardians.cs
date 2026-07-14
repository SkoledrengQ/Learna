using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learna.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReworkStudentAndAddGuardians : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GradeLevel",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ParentPhoneNumber",
                table: "Students");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Students",
                newName: "Name_LastName");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "Students",
                newName: "Name_FirstName");

            migrationBuilder.AddColumn<int>(
                name: "GuardianId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name_DisplayName",
                table: "Students",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Name_FirstNameEnglish",
                table: "Students",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Name_LastNameEnglish",
                table: "Students",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Name_Nickname",
                table: "Students",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Name_Title",
                table: "Students",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Guardians",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name_Title = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name_FirstName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name_LastName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name_FirstNameEnglish = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name_LastNameEnglish = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name_Nickname = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name_DisplayName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guardians", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "StudentGuardians",
                columns: table => new
                {
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    GuardianId = table.Column<int>(type: "int", nullable: false),
                    Relationship = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsPrimaryContact = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentGuardians", x => new { x.StudentId, x.GuardianId });
                    table.ForeignKey(
                        name: "FK_StudentGuardians_Guardians_GuardianId",
                        column: x => x.GuardianId,
                        principalTable: "Guardians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentGuardians_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Users_GuardianId",
                table: "Users",
                column: "GuardianId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentGuardians_GuardianId",
                table: "StudentGuardians",
                column: "GuardianId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Guardians_GuardianId",
                table: "Users",
                column: "GuardianId",
                principalTable: "Guardians",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Guardians_GuardianId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "StudentGuardians");

            migrationBuilder.DropTable(
                name: "Guardians");

            migrationBuilder.DropIndex(
                name: "IX_Users_GuardianId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GuardianId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Name_DisplayName",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Name_FirstNameEnglish",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Name_LastNameEnglish",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Name_Nickname",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Name_Title",
                table: "Students");

            migrationBuilder.RenameColumn(
                name: "Name_LastName",
                table: "Students",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "Name_FirstName",
                table: "Students",
                newName: "FirstName");

            migrationBuilder.AddColumn<int>(
                name: "GradeLevel",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ParentPhoneNumber",
                table: "Students",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
