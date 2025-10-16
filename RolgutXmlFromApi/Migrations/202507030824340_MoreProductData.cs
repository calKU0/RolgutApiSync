namespace GaskaSyncService.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class MoreProductData : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Products", "Description", c => c.String());
            AddColumn("dbo.Products", "Ean", c => c.String());
            AddColumn("dbo.Products", "TechnicalDetails", c => c.String());
            AddColumn("dbo.Products", "WeightNet", c => c.Single(nullable: false));
            AddColumn("dbo.Products", "WeightGross", c => c.Single(nullable: false));
        }

        public override void Down()
        {
            DropColumn("dbo.Products", "WeightGross");
            DropColumn("dbo.Products", "WeightNet");
            DropColumn("dbo.Products", "TechnicalDetails");
            DropColumn("dbo.Products", "Ean");
            DropColumn("dbo.Products", "Description");
        }
    }
}