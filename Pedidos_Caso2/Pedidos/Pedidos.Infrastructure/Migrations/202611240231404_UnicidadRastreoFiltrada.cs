namespace Pedidos.Infrastructure.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class UnicidadRastreoFiltrada : DbMigration
    {
        public override void Up()
        {
            // EF6 no genera un índice único filtrado; se crea con SQL (justificado en el README).
            DropIndex("dbo.Ordenes", "IX_Ordenes_NumeroRastreo");
            Sql("CREATE UNIQUE NONCLUSTERED INDEX [UX_Ordenes_NumeroRastreo] " +
                "ON [dbo].[Ordenes]([NumeroRastreo]) WHERE [NumeroRastreo] IS NOT NULL");
        }

        public override void Down()
        {
            Sql("DROP INDEX [UX_Ordenes_NumeroRastreo] ON [dbo].[Ordenes]");
            CreateIndex("dbo.Ordenes", "NumeroRastreo", name: "IX_Ordenes_NumeroRastreo");
        }
    }
}