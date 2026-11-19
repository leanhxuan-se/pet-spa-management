using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PetSpa.Modules.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "petspa_auth");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                schema: "petspa_auth",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                schema: "petspa_auth",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AccountType = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: true),
                    StaffId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ProvisioningState = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastLoginAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TokenVersion = table.Column<int>(type: "integer", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.CheckConstraint("ck_auth_user_phone", "\"PhoneNumber\" <> '' AND \"PhoneNumber\" !~ '^[[:space:]]|[[:space:]]$'");
                    table.CheckConstraint("ck_auth_user_profile", "(\"AccountType\" = 'customer' AND \"StaffId\" IS NULL AND (\"CustomerId\" IS NULL OR \"CustomerId\" > 0))\nOR (\"AccountType\" = 'staff' AND \"CustomerId\" IS NULL AND (\"StaffId\" IS NULL OR \"StaffId\" > 0))");
                    table.CheckConstraint("ck_auth_user_provisioning", "\"ProvisioningState\" = 'Pending'\nOR (\"ProvisioningState\" = 'Ready'\n    AND (\"CustomerId\" IS NOT NULL OR \"StaffId\" IS NOT NULL))");
                    table.CheckConstraint("ck_auth_user_token_version", "\"TokenVersion\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                schema: "petspa_auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "petspa_auth",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                schema: "petspa_auth",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "petspa_auth",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                schema: "petspa_auth",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "petspa_auth",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                schema: "petspa_auth",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "petspa_auth",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "petspa_auth",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                schema: "petspa_auth",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "petspa_auth",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "petspa_auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<byte[]>(type: "bytea", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.CheckConstraint("ck_refresh_token_expiration", "\"ExpiresAtUtc\" > \"CreatedAtUtc\"");
                    table.CheckConstraint("ck_refresh_token_hashes", "octet_length(\"TokenHash\") = 32\nAND (\"ReplacedByTokenHash\" IS NULL OR octet_length(\"ReplacedByTokenHash\") = 32)");
                    table.ForeignKey(
                        name: "FK_refresh_tokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "petspa_auth",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "petspa_auth",
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "admin", null, "admin", "ADMIN" },
                    { "customer", null, "customer", "CUSTOMER" },
                    { "manager", null, "manager", "MANAGER" },
                    { "receptionist", null, "receptionist", "RECEPTIONIST" },
                    { "staff", null, "staff", "STAFF" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                schema: "petspa_auth",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "petspa_auth",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                schema: "petspa_auth",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                schema: "petspa_auth",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                schema: "petspa_auth",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_UserId",
                schema: "petspa_auth",
                table: "AspNetUserRoles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "petspa_auth",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AccountType_PhoneNumber",
                schema: "petspa_auth",
                table: "AspNetUsers",
                columns: new[] { "AccountType", "PhoneNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CustomerId",
                schema: "petspa_auth",
                table: "AspNetUsers",
                column: "CustomerId",
                unique: true,
                filter: "\"CustomerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_StaffId",
                schema: "petspa_auth",
                table: "AspNetUsers",
                column: "StaffId",
                unique: true,
                filter: "\"StaffId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "petspa_auth",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_ExpiresAtUtc",
                schema: "petspa_auth",
                table: "refresh_tokens",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_FamilyId",
                schema: "petspa_auth",
                table: "refresh_tokens",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                schema: "petspa_auth",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                schema: "petspa_auth",
                table: "refresh_tokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims",
                schema: "petspa_auth");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims",
                schema: "petspa_auth");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins",
                schema: "petspa_auth");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles",
                schema: "petspa_auth");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens",
                schema: "petspa_auth");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "petspa_auth");

            migrationBuilder.DropTable(
                name: "AspNetRoles",
                schema: "petspa_auth");

            migrationBuilder.DropTable(
                name: "AspNetUsers",
                schema: "petspa_auth");
        }
    }
}
