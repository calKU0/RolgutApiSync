namespace RolgutXmlFromApi.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ChangeDataTypes : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Products", "InStock", c => c.Single(nullable: false));
            AlterColumn("dbo.Components", "Qty", c => c.Single(nullable: false));
            AlterColumn("dbo.Packages", "PackQty", c => c.Single(nullable: false));
            AlterColumn("dbo.RecommendedParts", "Qty", c => c.Single(nullable: false));
        }

        public override void Down()
        {
            AlterColumn("dbo.RecommendedParts", "Qty", c => c.Int(nullable: false));
            AlterColumn("dbo.Packages", "PackQty", c => c.Int(nullable: false));
            AlterColumn("dbo.Components", "Qty", c => c.Int(nullable: false));
            AlterColumn("dbo.Products", "InStock", c => c.Int(nullable: false));
        }
    }
}