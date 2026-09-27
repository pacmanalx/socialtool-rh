using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialTool.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SurveysAndStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "departments",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                // Áreas que já existiam viram "departamento"; áreas novas sempre informam o tipo.
                defaultValue: "Department")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "answer_scales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsNps = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsSystem = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedById = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_answer_scales", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "surveys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartsAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsAnonymous = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AudienceAll = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedById = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_surveys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_surveys_users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "answer_scale_options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ScaleId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_answer_scale_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_answer_scale_options_answer_scales_ScaleId",
                        column: x => x.ScaleId,
                        principalTable: "answer_scales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "survey_audience_areas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SurveyId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DepartmentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_audience_areas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_survey_audience_areas_departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_survey_audience_areas_surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "survey_participations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SurveyId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_participations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_survey_participations_surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_survey_participations_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "survey_questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SurveyId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ScaleName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsNps = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_survey_questions_surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "survey_responses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SurveyId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    AreaId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_responses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_survey_responses_surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_survey_responses_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "survey_question_options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_question_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_survey_question_options_survey_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "survey_questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "survey_answers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ResponseId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    QuestionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OptionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Value = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_survey_answers_survey_responses_ResponseId",
                        column: x => x.ResponseId,
                        principalTable: "survey_responses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_answer_scale_options_ScaleId",
                table: "answer_scale_options",
                column: "ScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_survey_answers_QuestionId",
                table: "survey_answers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_survey_answers_ResponseId",
                table: "survey_answers",
                column: "ResponseId");

            migrationBuilder.CreateIndex(
                name: "IX_survey_audience_areas_DepartmentId",
                table: "survey_audience_areas",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_survey_audience_areas_SurveyId_DepartmentId",
                table: "survey_audience_areas",
                columns: new[] { "SurveyId", "DepartmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_survey_participations_SurveyId_UserId",
                table: "survey_participations",
                columns: new[] { "SurveyId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_survey_participations_UserId",
                table: "survey_participations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_survey_question_options_QuestionId",
                table: "survey_question_options",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_survey_questions_SurveyId",
                table: "survey_questions",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_survey_responses_SurveyId",
                table: "survey_responses",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_survey_responses_UserId",
                table: "survey_responses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_surveys_CreatedById",
                table: "surveys",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_surveys_StartsAt_EndsAt",
                table: "surveys",
                columns: new[] { "StartsAt", "EndsAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "answer_scale_options");

            migrationBuilder.DropTable(
                name: "survey_answers");

            migrationBuilder.DropTable(
                name: "survey_audience_areas");

            migrationBuilder.DropTable(
                name: "survey_participations");

            migrationBuilder.DropTable(
                name: "survey_question_options");

            migrationBuilder.DropTable(
                name: "answer_scales");

            migrationBuilder.DropTable(
                name: "survey_responses");

            migrationBuilder.DropTable(
                name: "survey_questions");

            migrationBuilder.DropTable(
                name: "surveys");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "departments");
        }
    }
}
