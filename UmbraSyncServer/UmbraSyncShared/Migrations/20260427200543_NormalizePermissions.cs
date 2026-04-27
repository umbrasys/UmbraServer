using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MareSynchronosServer.Migrations
{
    /// <inheritdoc />
    public partial class NormalizePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Création des trois nouvelles tables AVANT toute manipulation des anciennes colonnes,
            //    pour pouvoir y migrer les données existantes.

            migrationBuilder.CreateTable(
                name: "group_pair_preferred_permissions",
                columns: table => new
                {
                    group_gid = table.Column<string>(type: "character varying(20)", nullable: false),
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false),
                    disable_animations = table.Column<bool>(type: "boolean", nullable: false),
                    disable_sounds = table.Column<bool>(type: "boolean", nullable: false),
                    disable_vfx = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group_pair_preferred_permissions", x => new { x.user_uid, x.group_gid });
                    table.ForeignKey(
                        name: "fk_group_pair_preferred_permissions_groups_group_gid",
                        column: x => x.group_gid,
                        principalTable: "groups",
                        principalColumn: "gid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_pair_preferred_permissions_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_default_preferred_permissions",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    disable_individual_animations = table.Column<bool>(type: "boolean", nullable: false),
                    disable_individual_sounds = table.Column<bool>(type: "boolean", nullable: false),
                    disable_individual_vfx = table.Column<bool>(type: "boolean", nullable: false),
                    disable_group_animations = table.Column<bool>(type: "boolean", nullable: false),
                    disable_group_sounds = table.Column<bool>(type: "boolean", nullable: false),
                    disable_group_vfx = table.Column<bool>(type: "boolean", nullable: false),
                    individual_is_sticky = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_default_preferred_permissions", x => x.user_uid);
                    table.ForeignKey(
                        name: "fk_user_default_preferred_permissions_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_permission_sets",
                columns: table => new
                {
                    user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    other_user_uid = table.Column<string>(type: "character varying(10)", nullable: false),
                    sticky = table.Column<bool>(type: "boolean", nullable: false),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false),
                    disable_animations = table.Column<bool>(type: "boolean", nullable: false),
                    disable_vfx = table.Column<bool>(type: "boolean", nullable: false),
                    disable_sounds = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_permission_sets", x => new { x.user_uid, x.other_user_uid });
                    table.ForeignKey(
                        name: "fk_user_permission_sets_users_other_user_uid",
                        column: x => x.other_user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_permission_sets_users_user_uid",
                        column: x => x.user_uid,
                        principalTable: "users",
                        principalColumn: "uid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_group_pair_preferred_permissions_group_gid",
                table: "group_pair_preferred_permissions",
                column: "group_gid");

            migrationBuilder.CreateIndex(
                name: "ix_group_pair_preferred_permissions_user_uid",
                table: "group_pair_preferred_permissions",
                column: "user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_user_permission_sets_other_user_uid",
                table: "user_permission_sets",
                column: "other_user_uid");

            migrationBuilder.CreateIndex(
                name: "ix_user_permission_sets_user_uid",
                table: "user_permission_sets",
                column: "user_uid");

            // 2) Migration des données client_pairs → user_permission_sets (sticky=true pour préserver les préfs existantes)
            migrationBuilder.Sql(@"
                INSERT INTO user_permission_sets (user_uid, other_user_uid, sticky, is_paused, disable_animations, disable_vfx, disable_sounds)
                SELECT cp.user_uid, cp.other_user_uid, true, cp.is_paused, cp.disable_animations, cp.disable_vfx, cp.disable_sounds
                FROM client_pairs cp
                ON CONFLICT (user_uid, other_user_uid) DO NOTHING;");

            // 3) Migration des données group_pairs → user_permission_sets (sticky=false, agrégation entre groupes communs)
            migrationBuilder.Sql(@"
                INSERT INTO user_permission_sets (user_uid, other_user_uid, sticky, is_paused, disable_animations, disable_vfx, disable_sounds)
                SELECT gp.group_user_uid, gp2.group_user_uid, false,
                    bool_and(gp.is_paused),
                    bool_and(g.disable_animations OR gp.disable_animations),
                    bool_and(g.disable_vfx OR gp.disable_vfx),
                    bool_and(g.disable_sounds OR gp.disable_sounds)
                FROM group_pairs gp
                LEFT JOIN group_pairs gp2 ON gp2.group_gid = gp.group_gid
                LEFT JOIN groups g ON g.gid = gp2.group_gid
                WHERE gp2.group_user_uid <> gp.group_user_uid
                GROUP BY gp.group_user_uid, gp2.group_user_uid
                ON CONFLICT (user_uid, other_user_uid) DO NOTHING;");

            // 4) Migration des données group_pairs → group_pair_preferred_permissions (combinaison group + group_pair)
            migrationBuilder.Sql(@"
                INSERT INTO group_pair_preferred_permissions (group_gid, user_uid, is_paused, disable_animations, disable_sounds, disable_vfx)
                SELECT gp.group_gid, gp.group_user_uid, gp.is_paused,
                    gp.disable_animations OR g.disable_animations AS disable_animations,
                    gp.disable_sounds OR g.disable_sounds AS disable_sounds,
                    gp.disable_vfx OR g.disable_vfx AS disable_vfx
                FROM group_pairs gp
                LEFT JOIN groups g ON g.gid = gp.group_gid
                ON CONFLICT (user_uid, group_gid) DO NOTHING;");

            // 5) Initialisation user_default_preferred_permissions pour tous les utilisateurs existants
            //    (tout à false par défaut, IndividualIsSticky=false). Évite les NullReferenceException
            //    dans les méthodes Hub qui font SingleAsync(uid==UserUID) sur cette table.
            migrationBuilder.Sql(@"
                INSERT INTO user_default_preferred_permissions (user_uid, disable_individual_animations, disable_individual_sounds, disable_individual_vfx, disable_group_animations, disable_group_sounds, disable_group_vfx, individual_is_sticky)
                SELECT uid, false, false, false, false, false, false, false
                FROM users
                ON CONFLICT (user_uid) DO NOTHING;");

            // 6) Maintenant on peut renommer les colonnes du Group et supprimer les colonnes obsolètes.
            migrationBuilder.RenameColumn(
                name: "disable_vfx",
                table: "groups",
                newName: "prefer_disable_vfx");

            migrationBuilder.RenameColumn(
                name: "disable_sounds",
                table: "groups",
                newName: "prefer_disable_sounds");

            migrationBuilder.RenameColumn(
                name: "disable_animations",
                table: "groups",
                newName: "prefer_disable_animations");

            migrationBuilder.DropColumn(
                name: "disable_animations",
                table: "group_pairs");

            migrationBuilder.DropColumn(
                name: "disable_sounds",
                table: "group_pairs");

            migrationBuilder.DropColumn(
                name: "disable_vfx",
                table: "group_pairs");

            migrationBuilder.DropColumn(
                name: "is_paused",
                table: "group_pairs");

            migrationBuilder.DropColumn(
                name: "allow_receiving_messages",
                table: "client_pairs");

            migrationBuilder.DropColumn(
                name: "disable_animations",
                table: "client_pairs");

            migrationBuilder.DropColumn(
                name: "disable_sounds",
                table: "client_pairs");

            migrationBuilder.DropColumn(
                name: "disable_vfx",
                table: "client_pairs");

            migrationBuilder.DropColumn(
                name: "is_paused",
                table: "client_pairs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: la rollback restaure le schéma uniquement, pas les données stockées dans
            // user_permission_sets / group_pair_preferred_permissions (perte de données acceptée
            // pour un rollback car les nouvelles tables sont droppées).

            migrationBuilder.AddColumn<bool>(
                name: "disable_animations",
                table: "group_pairs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "disable_sounds",
                table: "group_pairs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "disable_vfx",
                table: "group_pairs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_paused",
                table: "group_pairs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "allow_receiving_messages",
                table: "client_pairs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "disable_animations",
                table: "client_pairs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "disable_sounds",
                table: "client_pairs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "disable_vfx",
                table: "client_pairs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_paused",
                table: "client_pairs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.RenameColumn(
                name: "prefer_disable_vfx",
                table: "groups",
                newName: "disable_vfx");

            migrationBuilder.RenameColumn(
                name: "prefer_disable_sounds",
                table: "groups",
                newName: "disable_sounds");

            migrationBuilder.RenameColumn(
                name: "prefer_disable_animations",
                table: "groups",
                newName: "disable_animations");

            migrationBuilder.DropTable(
                name: "group_pair_preferred_permissions");

            migrationBuilder.DropTable(
                name: "user_default_preferred_permissions");

            migrationBuilder.DropTable(
                name: "user_permission_sets");
        }
    }
}
