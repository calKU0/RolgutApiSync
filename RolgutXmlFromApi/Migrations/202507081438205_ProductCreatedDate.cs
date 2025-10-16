namespace GaskaSyncService.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ProductCreatedDate : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Products", "CreatedDate", c => c.DateTime(nullable: false));
            AddColumn("dbo.Products", "UpdatedDate", c => c.DateTime(nullable: false));
        }

        public override void Down()
        {
            DropColumn("dbo.Products", "UpdatedDate");
            DropColumn("dbo.Products", "CreatedDate");
        }
    }
}