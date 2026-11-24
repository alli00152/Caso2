namespace Pedidos.Infrastructure.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CrearOrdenes : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Ordenes",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        ClienteNombre = c.String(nullable: false, maxLength: 150),
                        Estado = c.Int(nullable: false),
                        NumeroRastreo = c.String(maxLength: 100),
                        FechaCreacion = c.DateTime(nullable: false),
                        FechaEnvio = c.DateTime(),
                        FechaEntrega = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Ordenes");
        }
    }
}
