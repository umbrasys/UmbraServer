using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MareSynchronosServer.Migrations
{
    /// <inheritdoc />
    public partial class AddHousingScenarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "housing_scenarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    server_id = table.Column<long>(type: "bigint", nullable: false),
                    map_id = table.Column<long>(type: "bigint", nullable: false),
                    territory_id = table.Column<long>(type: "bigint", nullable: false),
                    division_id = table.Column<long>(type: "bigint", nullable: false),
                    ward_id = table.Column<long>(type: "bigint", nullable: false),
                    house_id = table.Column<long>(type: "bigint", nullable: false),
                    room_id = table.Column<long>(type: "bigint", nullable: false),
                    cipher_data = table.Column<byte[]>(type: "bytea", nullable: true),
                    nonce = table.Column<byte[]>(type: "bytea", nullable: true),
                    salt = table.Column<byte[]>(type: "bytea", nullable: true),
                    tag = table.Column<byte[]>(type: "bytea", nullable: true),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_housing_scenarios", x => x.id);
                    table.ForeignKey(
                        name: "fk_housing_scenarios_users_owner_uid",
                        column: x => x.owner_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "housing_scenario_allowed_groups",
                columns: table => new
                {
                    share_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allowed_group_gid = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_housing_scenario_allowed_groups", x => new { x.share_id, x.allowed_group_gid });
                    table.ForeignKey(
                        name: "fk_housing_scenario_allowed_groups_housing_scenarios_share_id",
                        column: x => x.share_id,
                        principalTable: "housing_scenarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "housing_scenario_allowed_users",
                columns: table => new
                {
                    share_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allowed_individual_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_housing_scenario_allowed_users", x => new { x.share_id, x.allowed_individual_uid });
                    table.ForeignKey(
                        name: "fk_housing_scenario_allowed_users_housing_scenarios_share_id",
                        column: x => x.share_id,
                        principalTable: "housing_scenarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_housing_scenario_allowed_groups_allowed_group_gid",
                table: "housing_scenario_allowed_groups",
                column: "allowed_group_gid");

            migrationBuilder.CreateIndex(
                name: "ix_housing_scenario_allowed_users_allowed_individual_uid",
                table: "housing_scenario_allowed_users",
                column: "allowed_individual_uid");

            migrationBuilder.CreateIndex(
                name: "ix_housing_scenarios_owner_uid",
                table: "housing_scenarios",
                column: "owner_uid");

            migrationBuilder.CreateIndex(
                name: "ix_housing_scenarios_server_id_territory_id_division_id_ward_i",
                table: "housing_scenarios",
                columns: new[] { "server_id", "territory_id", "division_id", "ward_id", "house_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "housing_scenario_allowed_groups");

            migrationBuilder.DropTable(
                name: "housing_scenario_allowed_users");

            migrationBuilder.DropTable(
                name: "housing_scenarios");
        }
    }
}
