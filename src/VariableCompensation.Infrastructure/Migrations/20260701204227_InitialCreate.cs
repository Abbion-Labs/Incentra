using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VariableCompensation.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "descriptive_ratings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MinAverage = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxAverage = table.Column<decimal>(type: "numeric", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_descriptive_ratings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "education_levels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_education_levels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "job_positions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_positions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "measure_types",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_measure_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "organization_units",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_units", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rating_levels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rating_levels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EmailVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "measure_type_descriptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MeasureTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_measure_type_descriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_measure_type_descriptions_measure_types_MeasureTypeId",
                        column: x => x.MeasureTypeId,
                        principalTable: "measure_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "variable_compensation_parameters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrganizationUnitId = table.Column<long>(type: "bigint", nullable: false),
                    Year = table.Column<short>(type: "smallint", nullable: false),
                    MonetaryPool = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UpperLimitCoefficient = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    DependencyWeight = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    Exponent = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variable_compensation_parameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_variable_compensation_parameters_organization_units_Organiz~",
                        column: x => x.OrganizationUnitId,
                        principalTable: "organization_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    OrganizationUnitId = table.Column<long>(type: "bigint", nullable: false),
                    JobPositionId = table.Column<long>(type: "bigint", nullable: false),
                    EducationLevelId = table.Column<long>(type: "bigint", nullable: true),
                    EvaluatorEmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    HiredAt = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employees_education_levels_EducationLevelId",
                        column: x => x.EducationLevelId,
                        principalTable: "education_levels",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_employees_employees_EvaluatorEmployeeId",
                        column: x => x.EvaluatorEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_employees_job_positions_JobPositionId",
                        column: x => x.JobPositionId,
                        principalTable: "job_positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employees_organization_units_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "organization_units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employees_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_roles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    EvaluatorEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    ControllerEmployeeId = table.Column<long>(type: "bigint", nullable: true),
                    Year = table.Column<short>(type: "smallint", nullable: false),
                    Quarter = table.Column<byte>(type: "smallint", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    GoalsAverage = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    MeasuresAverage = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    OverallAverage = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    DescriptiveRatingId = table.Column<long>(type: "bigint", nullable: true),
                    ConversationAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EvaluatorComment = table.Column<string>(type: "text", nullable: true),
                    ControllerComment = table.Column<string>(type: "text", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evaluations_descriptive_ratings_DescriptiveRatingId",
                        column: x => x.DescriptiveRatingId,
                        principalTable: "descriptive_ratings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_evaluations_employees_ControllerEmployeeId",
                        column: x => x.ControllerEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_evaluations_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evaluations_employees_EvaluatorEmployeeId",
                        column: x => x.EvaluatorEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evaluator_settings",
                columns: table => new
                {
                    EmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    ControllerEmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    ThresholdDoesNotMeet = table.Column<decimal>(type: "numeric", nullable: false),
                    ThresholdMeets = table.Column<decimal>(type: "numeric", nullable: false),
                    ThresholdGood = table.Column<decimal>(type: "numeric", nullable: false),
                    ThresholdExceeds = table.Column<decimal>(type: "numeric", nullable: false),
                    PercentDoesNotMeet = table.Column<decimal>(type: "numeric", nullable: false),
                    PercentMeets = table.Column<decimal>(type: "numeric", nullable: false),
                    PercentGood = table.Column<decimal>(type: "numeric", nullable: false),
                    PercentExceeds = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluator_settings", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_evaluator_settings_employees_ControllerEmployeeId",
                        column: x => x.ControllerEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evaluator_settings_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "variable_compensation_results",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    ParametersId = table.Column<long>(type: "bigint", nullable: false),
                    Year = table.Column<short>(type: "smallint", nullable: false),
                    GoalsAverage = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    MeasuresAverage = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    OverallAverage = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    SalaryPointsValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    ZScore = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    NormalizedZScore = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    NormalizedPoints = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    CompensationWithoutSalary = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Aq = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    NetCompensation = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    QuarterlyCompensation = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MonthlyCompensation = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CompensationPercent = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    FormulaVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsFinal = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variable_compensation_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_variable_compensation_results_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_variable_compensation_results_variable_compensation_paramet~",
                        column: x => x.ParametersId,
                        principalTable: "variable_compensation_parameters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluation_conditions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvaluationId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluation_conditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evaluation_conditions_evaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "evaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluation_criteria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvaluationId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluation_criteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evaluation_criteria_evaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "evaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluation_goals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvaluationId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RatingLevelId = table.Column<long>(type: "bigint", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    Weight = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluation_goals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evaluation_goals_evaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "evaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evaluation_goals_rating_levels_RatingLevelId",
                        column: x => x.RatingLevelId,
                        principalTable: "rating_levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluation_measures",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvaluationId = table.Column<long>(type: "bigint", nullable: false),
                    MeasureTypeId = table.Column<long>(type: "bigint", nullable: false),
                    MeasureDescriptionId = table.Column<long>(type: "bigint", nullable: true),
                    CustomDescription = table.Column<string>(type: "text", nullable: true),
                    RatingComment = table.Column<string>(type: "text", nullable: true),
                    RatingLevelId = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluation_measures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evaluation_measures_evaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "evaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evaluation_measures_measure_type_descriptions_MeasureDescri~",
                        column: x => x.MeasureDescriptionId,
                        principalTable: "measure_type_descriptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_evaluation_measures_measure_types_MeasureTypeId",
                        column: x => x.MeasureTypeId,
                        principalTable: "measure_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evaluation_measures_rating_levels_RatingLevelId",
                        column: x => x.RatingLevelId,
                        principalTable: "rating_levels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluation_status_history",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvaluationId = table.Column<long>(type: "bigint", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ChangedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluation_status_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evaluation_status_history_evaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "evaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluation_trainings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvaluationId = table.Column<long>(type: "bigint", nullable: false),
                    TrainingDescription = table.Column<string>(type: "text", nullable: true),
                    KnowledgeDescription = table.Column<string>(type: "text", nullable: true),
                    DevelopmentDescription = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluation_trainings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evaluation_trainings_evaluations_EvaluationId",
                        column: x => x.EvaluationId,
                        principalTable: "evaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "variable_compensation_evaluation_links",
                columns: table => new
                {
                    ResultId = table.Column<long>(type: "bigint", nullable: false),
                    EvaluationId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variable_compensation_evaluation_links", x => new { x.ResultId, x.EvaluationId });
                    table.ForeignKey(
                        name: "FK_variable_compensation_evaluation_links_evaluations_Evaluati~",
                        column: x => x.EvaluationId,
                        principalTable: "evaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_variable_compensation_evaluation_links_variable_compensatio~",
                        column: x => x.ResultId,
                        principalTable: "variable_compensation_results",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_CreatedAt",
                table: "audit_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_EntityType_EntityId",
                table: "audit_logs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_descriptive_ratings_Code",
                table: "descriptive_ratings",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_education_levels_Name",
                table: "education_levels",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_EducationLevelId",
                table: "employees",
                column: "EducationLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_EvaluatorEmployeeId",
                table: "employees",
                column: "EvaluatorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_IsActive",
                table: "employees",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_employees_JobPositionId",
                table: "employees",
                column: "JobPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_OrganizationUnitId",
                table: "employees",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_employees_UserId",
                table: "employees",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_conditions_EvaluationId",
                table: "evaluation_conditions",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_criteria_EvaluationId",
                table: "evaluation_criteria",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_goals_EvaluationId",
                table: "evaluation_goals",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_goals_RatingLevelId",
                table: "evaluation_goals",
                column: "RatingLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_measures_EvaluationId",
                table: "evaluation_measures",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_measures_MeasureDescriptionId",
                table: "evaluation_measures",
                column: "MeasureDescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_measures_MeasureTypeId",
                table: "evaluation_measures",
                column: "MeasureTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_measures_RatingLevelId",
                table: "evaluation_measures",
                column: "RatingLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_status_history_EvaluationId",
                table: "evaluation_status_history",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluation_trainings_EvaluationId",
                table: "evaluation_trainings",
                column: "EvaluationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_ControllerEmployeeId",
                table: "evaluations",
                column: "ControllerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_DescriptiveRatingId",
                table: "evaluations",
                column: "DescriptiveRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_EmployeeId_Year_Quarter",
                table: "evaluations",
                columns: new[] { "EmployeeId", "Year", "Quarter" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_EvaluatorEmployeeId",
                table: "evaluations",
                column: "EvaluatorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_Status",
                table: "evaluations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_evaluations_Year_Quarter",
                table: "evaluations",
                columns: new[] { "Year", "Quarter" });

            migrationBuilder.CreateIndex(
                name: "IX_evaluator_settings_ControllerEmployeeId",
                table: "evaluator_settings",
                column: "ControllerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_job_positions_Name",
                table: "job_positions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_measure_type_descriptions_MeasureTypeId",
                table: "measure_type_descriptions",
                column: "MeasureTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_measure_types_Code",
                table: "measure_types",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_units_Code",
                table: "organization_units",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_units_Name",
                table: "organization_units",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rating_levels_Value",
                table: "rating_levels",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_ExpiresAt",
                table: "refresh_tokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_roles_Code",
                table: "roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_RoleId",
                table: "user_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variable_compensation_evaluation_links_EvaluationId",
                table: "variable_compensation_evaluation_links",
                column: "EvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_variable_compensation_parameters_OrganizationUnitId_Year",
                table: "variable_compensation_parameters",
                columns: new[] { "OrganizationUnitId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variable_compensation_results_EmployeeId_ParametersId_Year",
                table: "variable_compensation_results",
                columns: new[] { "EmployeeId", "ParametersId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_variable_compensation_results_ParametersId",
                table: "variable_compensation_results",
                column: "ParametersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "evaluation_conditions");

            migrationBuilder.DropTable(
                name: "evaluation_criteria");

            migrationBuilder.DropTable(
                name: "evaluation_goals");

            migrationBuilder.DropTable(
                name: "evaluation_measures");

            migrationBuilder.DropTable(
                name: "evaluation_status_history");

            migrationBuilder.DropTable(
                name: "evaluation_trainings");

            migrationBuilder.DropTable(
                name: "evaluator_settings");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "variable_compensation_evaluation_links");

            migrationBuilder.DropTable(
                name: "measure_type_descriptions");

            migrationBuilder.DropTable(
                name: "rating_levels");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "evaluations");

            migrationBuilder.DropTable(
                name: "variable_compensation_results");

            migrationBuilder.DropTable(
                name: "measure_types");

            migrationBuilder.DropTable(
                name: "descriptive_ratings");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "variable_compensation_parameters");

            migrationBuilder.DropTable(
                name: "education_levels");

            migrationBuilder.DropTable(
                name: "job_positions");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "organization_units");
        }
    }
}
