using Pedidos.Application.Comun;
using Pedidos.Application.Contractos;
using Pedidos.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;

namespace Pedidos.Infrastructure
{
    public sealed class OrdenRepository : IOrdenRepository
    {
        private readonly PedidosDbContext _context;

        public OrdenRepository(PedidosDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Orden ObtenerPorId(Guid id)
        {
            // Con seguimiento: el servicio modifica la entidad y luego guarda los cambios.
            return _context.Ordenes
                .Include(o => o.Detalles)
                .FirstOrDefault(o => o.Id == id);
        }

        public Orden ObtenerPorRastreo(string numeroRastreoNormalizado)
        {
            return _context.Ordenes
                .Include(o => o.Detalles)
                .FirstOrDefault(o => o.NumeroRastreo == numeroRastreoNormalizado);
        }

        public IReadOnlyCollection<Orden> ObtenerTodas()
        {
            return _context.Ordenes
                .AsNoTracking()
                .OrderByDescending(o => o.FechaCreacion)
                .ToList()
                .AsReadOnly();
        }

        public IReadOnlyCollection<Orden> ObtenerPorPropietario(string applicationUserId)
        {
            return _context.Ordenes
                .AsNoTracking()
                .Where(o => o.ApplicationUserId == applicationUserId)
                .OrderByDescending(o => o.FechaCreacion)
                .ToList()
                .AsReadOnly();
        }

        public bool ExisteRastreo(string numeroRastreoNormalizado, Guid? excluirOrdenId)
        {
            var excluir = excluirOrdenId ?? Guid.Empty;
            return _context.Ordenes
                .AsNoTracking()
                .Any(o => o.NumeroRastreo == numeroRastreoNormalizado && o.Id != excluir);
        }

        public void Agregar(Orden orden)
        {
            _context.Ordenes.Add(orden);
        }

        public void EstablecerVersionOriginal(Orden orden, byte[] rowVersion)
        {
            // El UPDATE incluirá "WHERE RowVersion = <versión con la que el usuario abrió la pantalla>".
            _context.Entry(orden).Property(o => o.RowVersion).OriginalValue = rowVersion;
        }

        public void GuardarCambios()
        {
            try
            {
                // Un único SaveChanges se ejecuta en una sola transacción: la orden y sus detalles
                // se guardan juntos o no se guarda nada.
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new ConcurrenciaException(ex);
            }
            catch (DbUpdateException ex) when (EsViolacionUnicidad(ex))
            {
                throw new ViolacionUnicidadException(ex);
            }
        }

        private static bool EsViolacionUnicidad(Exception excepcion)
        {
            for (var actual = excepcion; actual != null; actual = actual.InnerException)
            {
                var sql = actual as SqlException;
                // 2601: índice único duplicado. 2627: restricción UNIQUE/PRIMARY KEY duplicada.
                if (sql != null && (sql.Number == 2601 || sql.Number == 2627))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
