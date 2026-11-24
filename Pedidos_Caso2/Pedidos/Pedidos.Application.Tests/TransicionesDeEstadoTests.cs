using FluentAssertions;
using Moq;
using Pedidos.Application.Comun;
using Pedidos.Application.Requests;
using Pedidos.Domain.Entities;
using Pedidos.Domain.Enums;
using System;
using Xunit;

namespace Pedidos.Application.Tests
{
    public class TransicionesDeEstadoTests
    {
        // ---------------------------------------------------------------- Reglas del Domain

        [Theory]
        [InlineData(EstadoOrden.Creada, EstadoOrden.Confirmada)]
        [InlineData(EstadoOrden.Confirmada, EstadoOrden.EnPreparacion)]
        [InlineData(EstadoOrden.EnPreparacion, EstadoOrden.Enviada)]
        [InlineData(EstadoOrden.Enviada, EstadoOrden.Entregada)]
        [InlineData(EstadoOrden.Creada, EstadoOrden.Cancelada)]
        [InlineData(EstadoOrden.Confirmada, EstadoOrden.Cancelada)]
        [InlineData(EstadoOrden.EnPreparacion, EstadoOrden.Cancelada)]
        public void EsTransicionValida_ConTransicionDelFlujo_DevuelveTrue(EstadoOrden origen, EstadoOrden destino)
        {
            // Arrange / Act
            var valida = Orden.EsTransicionValida(origen, destino);

            // Assert
            valida.Should().BeTrue();
        }

        [Theory]
        [InlineData(EstadoOrden.Creada, EstadoOrden.Enviada)]
        [InlineData(EstadoOrden.Creada, EstadoOrden.Entregada)]
        [InlineData(EstadoOrden.Confirmada, EstadoOrden.Enviada)]
        [InlineData(EstadoOrden.Enviada, EstadoOrden.Cancelada)]
        [InlineData(EstadoOrden.Enviada, EstadoOrden.Confirmada)]
        [InlineData(EstadoOrden.Entregada, EstadoOrden.Confirmada)]
        [InlineData(EstadoOrden.Cancelada, EstadoOrden.Confirmada)]
        public void EsTransicionValida_ConSaltoOMarchaAtras_DevuelveFalse(EstadoOrden origen, EstadoOrden destino)
        {
            var valida = Orden.EsTransicionValida(origen, destino);

            valida.Should().BeFalse();
        }

        [Fact]
        public void Enviar_DesdeCreada_LanzaInvalidOperationException()
        {
            // Arrange
            var orden = OrdenFabrica.Crear(estado: EstadoOrden.Creada);

            // Act
            Action accion = () => orden.Enviar("ABC123456");

            // Assert: el Domain protege el invariante aunque el servicio no lo verifique.
            accion.Should().Throw<InvalidOperationException>();
            orden.Estado.Should().Be(EstadoOrden.Creada);
        }

        // ---------------------------------------------------------------- Servicio

        [Fact]
        public void CambiarEstado_ConfirmarDesdeCreada_CambiaElEstadoYGuarda()
        {
            // Arrange
            var fx = new ServicioFixture();
            fx.ComoOperador();
            var orden = OrdenFabrica.Crear().ConVersion(ServicioFixture.Version);
            fx.DevuelveOrden(orden);

            // Act
            var resultado = fx.Servicio.CambiarEstado(new CambiarEstadoOrdenRequest
            {
                OrdenId = orden.Id,
                EstadoSolicitado = EstadoOrden.Confirmada,
                RowVersion = ServicioFixture.Version
            });

            // Assert
            resultado.EsExitoso.Should().BeTrue();
            orden.Estado.Should().Be(EstadoOrden.Confirmada);
            fx.Repositorio.Verify(r => r.EstablecerVersionOriginal(orden, ServicioFixture.Version), Times.Once);
            fx.Repositorio.Verify(r => r.GuardarCambios(), Times.Once);
        }

        [Fact]
        public void CambiarEstado_TransicionInvalida_DevuelveConflictoYNoGuarda()
        {
            // Arrange: una orden Creada no puede pasar directamente a Entregada.
            var fx = new ServicioFixture();
            fx.ComoOperador();
            var orden = OrdenFabrica.Crear().ConVersion(ServicioFixture.Version);
            fx.DevuelveOrden(orden);

            // Act
            var resultado = fx.Servicio.CambiarEstado(new CambiarEstadoOrdenRequest
            {
                OrdenId = orden.Id,
                EstadoSolicitado = EstadoOrden.Entregada,
                RowVersion = ServicioFixture.Version
            });

            // Assert
            resultado.EsExitoso.Should().BeFalse();
            resultado.TipoError.Should().Be(TipoError.Conflicto);
            orden.Estado.Should().Be(EstadoOrden.Creada);
            fx.Repositorio.Verify(r => r.GuardarCambios(), Times.Never);
        }

        [Fact]
        public void CambiarEstado_ConVersionDesactualizada_DevuelveConflictoDeConcurrencia()
        {
            // Arrange: otra persona ya modificó la orden (la versión actual es distinta).
            var fx = new ServicioFixture();
            fx.ComoOperador();
            var orden = OrdenFabrica.Crear().ConVersion(new byte[] { 9, 9, 9, 9 });
            fx.DevuelveOrden(orden);

            // Act
            var resultado = fx.Servicio.CambiarEstado(new CambiarEstadoOrdenRequest
            {
                OrdenId = orden.Id,
                EstadoSolicitado = EstadoOrden.Confirmada,
                RowVersion = ServicioFixture.Version
            });

            // Assert: no se sobrescribe silenciosamente.
            resultado.TipoError.Should().Be(TipoError.Conflicto);
            resultado.Mensaje.Should().Be(Servicios.OrdenServicio.MensajeConcurrencia);
            fx.Repositorio.Verify(r => r.GuardarCambios(), Times.Never);
        }

        [Fact]
        public void CambiarEstado_CuandoLaBaseDetectaConcurrencia_DevuelveConflicto()
        {
            // Arrange: la versión coincide al leer, pero el UPDATE falla porque alguien guardó antes.
            var fx = new ServicioFixture();
            fx.ComoOperador();
            var orden = OrdenFabrica.Crear().ConVersion(ServicioFixture.Version);
            fx.DevuelveOrden(orden);
            fx.Repositorio.Setup(r => r.GuardarCambios()).Throws(new ConcurrenciaException(new Exception()));

            // Act
            var resultado = fx.Servicio.CambiarEstado(new CambiarEstadoOrdenRequest
            {
                OrdenId = orden.Id,
                EstadoSolicitado = EstadoOrden.Confirmada,
                RowVersion = ServicioFixture.Version
            });

            // Assert
            resultado.TipoError.Should().Be(TipoError.Conflicto);
        }

        [Fact]
        public void CambiarEstado_OrdenInexistente_DevuelveNoEncontrado()
        {
            var fx = new ServicioFixture();
            fx.ComoOperador();

            var resultado = fx.Servicio.CambiarEstado(new CambiarEstadoOrdenRequest
            {
                OrdenId = Guid.NewGuid(),
                EstadoSolicitado = EstadoOrden.Confirmada,
                RowVersion = ServicioFixture.Version
            });

            resultado.TipoError.Should().Be(TipoError.NoEncontrado);
        }

        [Fact]
        public void Enviar_DesdeEnPreparacion_NormalizaElRastreoYCambiaAEnviada()
        {
            // Arrange
            var fx = new ServicioFixture();
            fx.ComoOperador();
            var orden = OrdenFabrica.Crear(estado: EstadoOrden.EnPreparacion).ConVersion(ServicioFixture.Version);
            fx.DevuelveOrden(orden);

            // Act
            var resultado = fx.Servicio.Enviar(new EnviarOrdenRequest
            {
                OrdenId = orden.Id,
                NumeroRastreo = "  ab-123 456 ",
                RowVersion = ServicioFixture.Version
            });

            // Assert
            resultado.EsExitoso.Should().BeTrue();
            orden.Estado.Should().Be(EstadoOrden.Enviada);
            orden.NumeroRastreo.Should().Be("AB-123456");
        }

        [Fact]
        public void Enviar_ConRastreoYaAsignadoAOtraOrden_DevuelveConflicto()
        {
            var fx = new ServicioFixture();
            fx.ComoOperador();
            var orden = OrdenFabrica.Crear(estado: EstadoOrden.EnPreparacion).ConVersion(ServicioFixture.Version);
            fx.DevuelveOrden(orden);
            fx.Repositorio.Setup(r => r.ExisteRastreo("ABC123456", orden.Id)).Returns(true);

            var resultado = fx.Servicio.Enviar(new EnviarOrdenRequest
            {
                OrdenId = orden.Id,
                NumeroRastreo = "abc123456",
                RowVersion = ServicioFixture.Version
            });

            resultado.TipoError.Should().Be(TipoError.Conflicto);
            fx.Repositorio.Verify(r => r.GuardarCambios(), Times.Never);
        }
    }
}
