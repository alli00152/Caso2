using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pedidos.Application.Contractos;
using Pedidos.Application.Mappings;
using Pedidos.Application.Servicios;
using Pedidos.Application.Validadores;
using Pedidos.Domain.Entities;
using Pedidos.Domain.Enums;
using System;

namespace Pedidos.Application.Tests
{
    /// <summary>Crea órdenes de prueba en el estado deseado, usando solo las operaciones del Domain.</summary>
    internal static class OrdenFabrica
    {
        public static Orden Crear(string propietario = "usuario-1", EstadoOrden estado = EstadoOrden.Creada)
        {
            var orden = new Orden("Cliente de prueba", "Avenida Central 123, San José", propietario);
            orden.AgregarDetalle("Teclado", 2);

            if (estado >= EstadoOrden.Confirmada && estado != EstadoOrden.Cancelada) orden.Confirmar();
            if (estado >= EstadoOrden.EnPreparacion && estado != EstadoOrden.Cancelada) orden.IniciarPreparacion();
            if (estado >= EstadoOrden.Enviada && estado != EstadoOrden.Cancelada) orden.Enviar("ABC123456");
            if (estado == EstadoOrden.Entregada) orden.Entregar();
            if (estado == EstadoOrden.Cancelada) orden.Cancelar();

            return orden;
        }

        /// <summary>Simula la versión (rowversion) que asigna la base de datos.</summary>
        public static Orden ConVersion(this Orden orden, byte[] version)
        {
            typeof(Orden).GetProperty("RowVersion").SetValue(orden, version);
            return orden;
        }
    }

    /// <summary>Servicio real con repositorio y usuario simulados (sin SQL Server ni Identity).</summary>
    internal sealed class ServicioFixture
    {
        public static readonly byte[] Version = { 1, 2, 3, 4 };

        public ServicioFixture()
        {
            Repositorio = new Mock<IOrdenRepository>();
            Usuario = new Mock<IUsuarioActual>();

            var config = new MapperConfiguration(c => c.AddProfile<ApplicationMappingProfile>(), NullLoggerFactory.Instance);
            Servicio = new OrdenServicio(
                Repositorio.Object,
                Usuario.Object,
                config.CreateMapper(),
                new CrearOrdenRequestValidator(),
                new EnviarOrdenRequestValidator(),
                new CambiarEstadoOrdenRequestValidator());
        }

        public Mock<IOrdenRepository> Repositorio { get; private set; }
        public Mock<IUsuarioActual> Usuario { get; private set; }
        public OrdenServicio Servicio { get; private set; }

        public void ComoCliente(string id) { Configurar(id, false, false); }
        public void ComoOperador(string id = "operador-1") { Configurar(id, false, true); }
        public void ComoAdministrador(string id = "admin-1") { Configurar(id, true, false); }
        public void ComoAnonimo() { Usuario.SetupGet(u => u.EstaAutenticado).Returns(false); }

        public void DevuelveOrden(Orden orden)
        {
            Repositorio.Setup(r => r.ObtenerPorId(orden.Id)).Returns(orden);
        }

        private void Configurar(string id, bool admin, bool operador)
        {
            Usuario.SetupGet(u => u.EstaAutenticado).Returns(true);
            Usuario.SetupGet(u => u.Id).Returns(id);
            Usuario.SetupGet(u => u.EsAdministrador).Returns(admin);
            Usuario.SetupGet(u => u.EsOperador).Returns(operador);
        }
    }
}
