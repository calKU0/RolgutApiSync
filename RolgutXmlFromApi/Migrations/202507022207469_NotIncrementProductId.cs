namespace GaskaSyncService.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class NotIncrementProductId : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ProductCategories", "ProductId", "dbo.Products");
            DropForeignKey("dbo.Components", "ProductId", "dbo.Products");
            DropForeignKey("dbo.CrossNumbers", "ProductId", "dbo.Products");
            DropForeignKey("dbo.ProductFiles", "ProductId", "dbo.Products");
            DropForeignKey("dbo.ProductImages", "ProductId", "dbo.Products");
            DropForeignKey("dbo.Packages", "ProductId", "dbo.Products");
            DropForeignKey("dbo.ProductParameters", "ProductId", "dbo.Products");
            DropForeignKey("dbo.RecommendedParts", "ProductId", "dbo.Products");
            DropForeignKey("dbo.Applications", "ProductId", "dbo.Products");
            DropPrimaryKey("dbo.Products");
            AlterColumn("dbo.Products", "Id", c => c.Int(nullable: false));
            AddPrimaryKey("dbo.Products", "Id");
            AddForeignKey("dbo.ProductCategories", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Components", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
            AddForeignKey("dbo.CrossNumbers", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
            AddForeignKey("dbo.ProductFiles", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
            AddForeignKey("dbo.ProductImages", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Packages", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
            AddForeignKey("dbo.ProductParameters", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
            AddForeignKey("dbo.RecommendedParts", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Applications", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
        }

        public override void Down()
        {
            DropForeignKey("dbo.Applications", "ProductId", "dbo.Products");
            DropForeignKey("dbo.RecommendedParts", "ProductId", "dbo.Products");
            DropForeignKey("dbo.ProductParameters", "ProductId", "dbo.Products");
            DropForeignKey("dbo.Packages", "ProductId", "dbo.Products");
            DropForeignKey("dbo.ProductImages", "ProductId", "dbo.Products");
            DropForeignKey("dbo.ProductFiles", "ProductId", "dbo.Products");
            DropForeignKey("dbo.CrossNumbers", "ProductId", "dbo.Products");
            DropForeignKey("dbo.Components", "ProductId", "dbo.Products");
            DropForeignKey("dbo.ProductCategories", "ProductId", "dbo.Products");
            DropPrimaryKey("dbo.Products");
            AlterColumn("dbo.Products", "Id", c => c.Int(nullable: false, identity: true));
            AddPrimaryKey("dbo.Products", "Id");
            AddForeignKey("dbo.Applications", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
            AddForeignKey("dbo.RecommendedParts", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
            AddForeignKey("dbo.ProductParameters", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Packages", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
            AddForeignKey("dbo.ProductImages", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
            AddForeignKey("dbo.ProductFiles", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
            AddForeignKey("dbo.CrossNumbers", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
            AddForeignKey("dbo.Components", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
            AddForeignKey("dbo.ProductCategories", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
        }
    }
}