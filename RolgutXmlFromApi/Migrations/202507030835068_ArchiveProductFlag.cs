namespace GaskaSyncService.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ArchiveProductFlag : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Products", "Archived", c => c.Boolean(nullable: false));
        }

        public override void Down()
        {
            DropColumn("dbo.Products", "Archived");
        }
    }
}