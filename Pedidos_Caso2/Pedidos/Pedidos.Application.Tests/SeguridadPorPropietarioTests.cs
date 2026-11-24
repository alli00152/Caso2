using FluentAssertions;
using Moq;
using Pedidos.Application.Comun;
using Pedidos.Application.Requests;
using Pedidos.Domain.Entities;
using Pedidos.Domain.Enums;
using System.Collections.Generic;
using Xunit;

namespace Pedidos.Application.Tests
{
    public class SeguridadPorPropietarioTests
    {
        [Fact]
        public void ObtenerPorId_ClienteConsultaOrdenAjena_DevuelveProhibido()
        {
            // Arrange: la orden pertenece a otro usuario.
            var fx = new ServicioFixture();
            fx.ComoCliente("cliente-B");
            var ordenDeA = OrdenFabrica.Crear(propietario: "cliente-A");
            fx.DevuelveOrden(ordenDeA);

            // Act
            var resultado = fx.Servicio.ObtenerPorId(ordenDeA.Id);

            // Assert: [Authorize] no basta; el servicio valida la propiedad en el servidor.
            resultado.EsExitoso.Should().BeFalse();
            resultado.TipoError.Should().Be(TipoError.Prohibido);
            resultado.Valor.Should().BeNull();
        }

        [Fact]
        public void ObtenerPorId_ClienteConsultaSuPropiaOrden_DevuelveLaOrden()
        {
            var fx = new ServicioFixture();
            fx.ComoCliente("cliente-A");
            var orden = OrdenFabrica.Crear(propietario: "cliente-A");
            fx.DevuelveOrden(orden);

            var resultado = fx.Servicio.ObtenerPorId(orden.Id);

            resultado.EsExitoso.Should().BeTrue();
            resultado.Valor.Id.Should().Be(orden.Id);
            resultado.Valor.Estado.Should().Be("Creada");
        }

        [Fact]
        public void ObtenerPorId_OperadorConsultaOrdenDeCualquierCliente_DevuelveLaOrden()
        {
            var fx = new ServicioFixture();
            fx.ComoOperador();
            var orden = OrdenFabrica.Crear(propietario: "cliente-A");
            fx.DevuelveOrden(orden);

            fx.Servicio.ObtenerPorId(orden.Id).EsExitoso.Should().BeTrue();
        }

        [Fact]
        public void VerificarAcceso_ClienteConOrdenAjena_DevuelveProhibido()
        {
            var fx = new ServicioFixture();
            fx.ComoCliente("cliente-B");
            var orden = OrdenFabrica.Crear(propietario: "cliente-A");
            fx.DevuelveOrden(orden);

            fx.Servicio.VerificarAcceso(orden.Id).TipoError.Should().Be(TipoError.Prohibido);
        }

        [Fact]
        public void ObtenerListado_Cliente_FiltraPorPropietarioEnElRepositorio()
        {
            var fx = new ServicioFixture();
            fx.ComoCliente("cliente-A");
            fx.Repositorio.Setup(r => r.ObtenerPorPropietario("cliente-A"))
                .Returns(new List<Orden> { OrdenFabrica.Crear(propietario: "cliente-A") });

            var listado = fx.Servicio.ObtenerListado();

            listado.Should().HaveCount(1);
            fx.Repositorio.Verify(r => r.ObtenerPorPropietario("cliente-A"), Times.Once);
            fx.Repositorio.Verify(r => r.ObtenerTodas(), Times.Never);
        }

        [Fact]
        public void ConsultarRastreo_ClienteConNumeroDeOrdenAjena_DevuelveNoEncontrado()
        {
            // Arrange: no se debe confirmar la existencia de rastreos de otras personas.
            var fx = new ServicioFixture();
            fx.ComoCliente("cliente-B");
            var ordenDeA = OrdenFabrica.Crear(propietario: "cliente-A", estado: EstadoOrden.Enviada);
            fx.Repositorio.Setup(r => r.ObtenerPorRastreo("ABC123456")).Returns(ordenDeA);

            var resultado = fx.Servicio.ConsultarRastreo("abc123456");

            resultado.TipoError.Should().Be(TipoError.NoEncontrado);
        }

        [Fact]
        public void ConsultarRastreo_PorIdentificadorPublicoDeSuOrden_DevuelveLaOrden()
        {
            var fx = new ServicioFixture();
            fx.ComoCliente("cliente-A");
            var orden = OrdenFabrica.Crear(propietario: "cliente-A", estado: EstadoOrden.Enviada);
            fx.DevuelveOrden(orden);

            var resultado = fx.Servicio.ConsultarRastreo(orden.Id.ToString());

            resultado.EsExitoso.Should().BeTrue();
        }

        [Fact]
        public void CambiarEstado_ClienteIntentaConfirmar_DevuelveProhibidoSinTocarElRepositorio()
        {
            var fx = new ServicioFixture();
            fx.ComoCliente("cliente-A");
            var orden = OrdenFabrica.Crear(propietario: "cliente-A").ConVersion(ServicioFixture.Version);
            fx.DevuelveOrden(orden);

            var resultado = fx.Servicio.CambiarEstado(new CambiarEstadoOrdenRequest
            {
                OrdenId = orden.Id,
                EstadoSolicitado = EstadoOrden.Confirmada,
                RowVersion = ServicioFixture.Version
            });

            resultado.TipoError.Should().Be(TipoError.Prohibido);
            fx.Repositorio.Verify(r => r.GuardarCambios(), Times.Never);
            orden.Estado.Should().Be(EstadoOrden.Creada);
        }

        [Fact]
        public void CambiarEstado_OperadorIntentaCancelar_DevuelveProhibido()
        {
            var fx = new ServicioFixture();
            fx.ComoOperador();
            var orden = OrdenFabrica.Crear().ConVersion(ServicioFixture.Version);
            fx.DevuelveOrden(orden);

            var resultado = fx.Servicio.CambiarEstado(new CambiarEstadoOrdenRequest
            {
                OrdenId = orden.Id,
                EstadoSolicitado = EstadoOrden.Cancelada,
                RowVersion = ServicioFixture.Version
            });

            resultado.TipoError.Should().Be(TipoError.Prohibido);
            orden.Estado.Should().Be(EstadoOrden.Creada);
        }

        [Fact]
        public void CambiarEstado_AdministradorCancela_CambiaAEstadoCancelada()
        {
            var fx = new ServicioFixture();
            fx.ComoAdministrador();
            var orden = OrdenFabrica.Crear().ConVersion(ServicioFixture.Version);
            fx.DevuelveOrden(orden);

            var resultado = fx.Servicio.CambiarEstado(new CambiarEstadoOrdenRequest
            {
                OrdenId = orden.Id,
                EstadoSolicitado = EstadoOrden.Cancelada,
                RowVersion = ServicioFixture.Version
            });

            resultado.EsExitoso.Should().BeTrue();
            orden.Estado.Should().Be(EstadoOrden.Cancelada);
        }

        [Fact]
        public void Crear_AsignaComoPropietarioAlUsuarioAutenticado()
        {
            // Arrange: el propietario nunca viene del formulario.
            var fx = new ServicioFixture();
            fx.ComoCliente("cliente-A");
            Orden creada = null;
            fx.Repositorio.Setup(r => r.Agregar(It.IsAny<Orden>())).Callback<Orden>(o => creada = o);

            // Act
            var resultado = fx.Servicio.Crear(new CrearOrdenRequest
            {
                ClienteNombre = "Ana Mora",
                DireccionEntrega = "Avenida Central 123",
                Detalles = new List<DetalleOrdenRequest> { new DetalleOrdenRequest { Producto = "Mouse", Cantidad = 1 } }
            });

            // Assert
            resultado.EsExitoso.Should().BeTrue();
            creada.ApplicationUserId.Should().Be("cliente-A");
            creada.Detalles.Should().HaveCount(1);
            fx.Repositorio.Verify(r => r.GuardarCambios(), Times.Once);
        }

        [Fact]
        public void Crear_ConDatosInvalidos_DevuelveValidacionYNoGuarda()
        {
            var fx = new ServicioFixture();
            fx.ComoCliente("cliente-A");

            var resultado = fx.Servicio.Crear(new CrearOrdenRequest { ClienteNombre = "A", DireccionEntrega = "x" });

            resultado.TipoError.Should().Be(TipoError.Validacion);
            resultado.Errores.Should().NotBeEmpty();
            fx.Repositorio.Verify(r => r.Agregar(It.IsAny<Orden>()), Times.Never);
        }

        [Fact]
        public void Crear_UsuarioAnonimo_DevuelveProhibido()
        {
            var fx = new ServicioFixture();
            fx.ComoAnonimo();

            var resultado = fx.Servicio.Crear(new CrearOrdenRequest());

            resultado.TipoError.Should().Be(TipoError.Prohibido);
        }
    }
}
