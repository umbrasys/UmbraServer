using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MareSynchronosServer.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrichedProfileJsonAndVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "enriched_profile_json",
                table: "character_rp_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "enriched_profile_visibility",
                table: "character_rp_profiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "enriched_profile_json",
                table: "character_rp_profiles");

            migrationBuilder.DropColumn(
                name: "enriched_profile_visibility",
                table: "character_rp_profiles");
        }
    }
}
