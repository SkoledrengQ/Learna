using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Learna.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentsAndSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_FileResource_ExactlyOneTarget",
                table: "FileResources");

            migrationBuilder.AddColumn<int>(
                name: "AssignmentId",
                table: "FileResources",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionId",
                table: "FileResources",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SubjectGroupId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DeadlineDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DeadlineTime = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    LatePolicy = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assignments_SubjectGroups_SubjectGroupId",
                        column: x => x.SubjectGroupId,
                        principalTable: "SubjectGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Assignments_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AssignmentExtensions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    ExtendedDeadlineDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExtendedDeadlineTime = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    Note = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentExtensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentExtensions_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignmentExtensions_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Text = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsLate = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Submissions_Assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Submissions_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_FileResources_AssignmentId",
                table: "FileResources",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FileResources_SubmissionId",
                table: "FileResources",
                column: "SubmissionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FileResource_ExactlyOneTarget",
                table: "FileResources",
                sql: "((`SubjectGroupId` IS NOT NULL) + (`LessonId` IS NOT NULL) + (`AssignmentId` IS NOT NULL) + (`SubmissionId` IS NOT NULL)) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentExtensions_AssignmentId_StudentId",
                table: "AssignmentExtensions",
                columns: new[] { "AssignmentId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentExtensions_StudentId",
                table: "AssignmentExtensions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_CreatedByUserId",
                table: "Assignments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_SubjectGroupId",
                table: "Assignments",
                column: "SubjectGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AssignmentId_StudentId",
                table: "Submissions",
                columns: new[] { "AssignmentId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_StudentId",
                table: "Submissions",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_FileResources_Assignments_AssignmentId",
                table: "FileResources",
                column: "AssignmentId",
                principalTable: "Assignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FileResources_Submissions_SubmissionId",
                table: "FileResources",
                column: "SubmissionId",
                principalTable: "Submissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileResources_Assignments_AssignmentId",
                table: "FileResources");

            migrationBuilder.DropForeignKey(
                name: "FK_FileResources_Submissions_SubmissionId",
                table: "FileResources");

            migrationBuilder.DropTable(
                name: "AssignmentExtensions");

            migrationBuilder.DropTable(
                name: "Submissions");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.DropIndex(
                name: "IX_FileResources_AssignmentId",
                table: "FileResources");

            migrationBuilder.DropIndex(
                name: "IX_FileResources_SubmissionId",
                table: "FileResources");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FileResource_ExactlyOneTarget",
                table: "FileResources");

            migrationBuilder.DropColumn(
                name: "AssignmentId",
                table: "FileResources");

            migrationBuilder.DropColumn(
                name: "SubmissionId",
                table: "FileResources");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FileResource_ExactlyOneTarget",
                table: "FileResources",
                sql: "(`SubjectGroupId` IS NULL) <> (`LessonId` IS NULL)");
        }
    }
}
