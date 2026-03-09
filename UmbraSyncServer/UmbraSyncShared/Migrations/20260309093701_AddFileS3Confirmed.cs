using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MareSynchronosServer.Migrations
{
    /// <inheritdoc />
    public partial class AddFileS3Confirmed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // S3 columns — IF NOT EXISTS car possiblement déjà ajoutées manuellement en prod
            migrationBuilder.Sql("""
                ALTER TABLE file_caches ADD COLUMN IF NOT EXISTS s3confirmed boolean NOT NULL DEFAULT false;
                ALTER TABLE file_caches ADD COLUMN IF NOT EXISTS s3confirmed_at timestamp with time zone;
                CREATE INDEX IF NOT EXISTS ix_file_caches_s3confirmed ON file_caches (s3confirmed);
                """);

            migrationBuilder.AddColumn<string>(
                name: "moodles_data",
                table: "character_rp_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "housing_share_allowed_groups",
                columns: table => new
                {
                    share_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allowed_group_gid = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_housing_share_allowed_groups", x => new { x.share_id, x.allowed_group_gid });
                    table.ForeignKey(
                        name: "fk_housing_share_allowed_groups_housing_shares_share_id",
                        column: x => x.share_id,
                        principalTable: "housing_shares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "housing_share_allowed_users",
                columns: table => new
                {
                    share_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allowed_individual_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_housing_share_allowed_users", x => new { x.share_id, x.allowed_individual_uid });
                    table.ForeignKey(
                        name: "fk_housing_share_allowed_users_housing_shares_share_id",
                        column: x => x.share_id,
                        principalTable: "housing_shares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_housing_share_allowed_groups_allowed_group_gid",
                table: "housing_share_allowed_groups",
                column: "allowed_group_gid");

            migrationBuilder.CreateIndex(
                name: "ix_housing_share_allowed_users_allowed_individual_uid",
                table: "housing_share_allowed_users",
                column: "allowed_individual_uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "housing_share_allowed_groups");

            migrationBuilder.DropTable(
                name: "housing_share_allowed_users");

            migrationBuilder.DropIndex(
                name: "ix_file_caches_s3confirmed",
                table: "file_caches");

            migrationBuilder.DropColumn(
                name: "s3confirmed",
                table: "file_caches");

            migrationBuilder.DropColumn(
                name: "s3confirmed_at",
                table: "file_caches");

            migrationBuilder.DropColumn(
                name: "moodles_data",
                table: "character_rp_profiles");
        }
    }
}
