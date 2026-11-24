using FluentAssertions;
using Pedidos.Application.Requests;
using Pedidos.Application.Validadores;
using Pedidos.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Pedidos.Application.Tests
{
    public class CrearOrdenRequestValidatorTests
    {
        private readonly CrearOrdenRequestValidator _validador = new CrearOrdenRequestValidator();

        private static CrearOrdenRequest SolicitudValida()
        {
            return new CrearOrdenRequest
            {
                ClienteNombre = "Ana Mora",
                DireccionEntrega = "Avenida Central 123",
                Detalles = new List<DetalleOrdenRequest>
                {
                    new DetalleOrdenRequest { Producto = "Teclado", Cantidad = 2 }
                }
            };
        }

        private List<string> Propiedades(CrearOrdenRequest solicitud)
        {
            return _validador.Validate(solicitud).Errors.Select(e => e.PropertyName).ToList();
        }

        [Fact]
        public void Validate_ConSolicitudCompleta_EsValida()
        {
            _validador.Validate(SolicitudValida()).IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("Ab", false)]
        [InlineData("Abc", true)]
        public void Cliente_ObligatorioYConMinimoDeTresCaracteres(string cliente, bool esValido)
        {
            var solicitud = SolicitudValida();
            solicitud.ClienteNombre = cliente;

            Propiedades(solicitud).Contains("ClienteNombre").Should().Be(!esValido);
        }

        [Fact]
        public void Cliente_ConMasDeCientoVeinteCaracteres_NoEsValido()
        {
            var solicitud = SolicitudValida();
            solicitud.ClienteNombre = new string('a', 121);

            Propiedades(solicitud).Should().Contain("ClienteNombre");
        }

        [Fact]
        public void Cliente_ConExactamenteCientoVeinteCaracteres_EsValido()
        {
            var solicitud = SolicitudValida();
            solicitud.ClienteNombre = new string('a', 120);

            Propiedades(solicitud).Should().NotContain("ClienteNombre");
        }

        [Theory]
        [InlineData(9, false)]
        [InlineData(10, true)]
        [InlineData(250, true)]
        [InlineData(251, false)]
        public void Direccion_DebeTenerEntreDiezYDoscientosCincuentaCaracteres(int longitud, bool esValida)
        {
            var solicitud = SolicitudValida();
            solicitud.DireccionEntrega = new string('d', longitud);

            Propiedades(solicitud).Contains("DireccionEntrega").Should().Be(!esValida);
        }

        [Fact]
        public void Detalles_SinNingunDetalle_NoEsValido()
        {
            var solicitud = SolicitudValida();
            solicitud.Detalles = new List<DetalleOrdenRequest>();

            Propiedades(solicitud).Should().Contain("Detalles");
        }

        [Fact]
        public void Detalle_SinProducto_NoEsValido()
        {
            var solicitud = SolicitudValida();
            solicitud.Detalles[0].Producto = " ";

            Propiedades(solicitud).Should().Contain("Detalles[0].Producto");
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(99, true)]
        [InlineData(100, false)]
        public void Detalle_CantidadDebeEstarEntreUnoYNoventaYNueve(int cantidad, bool esValida)
        {
            var solicitud = SolicitudValida();
            solicitud.Detalles[0].Cantidad = cantidad;

            Propiedades(solicitud).Contains("Detalles[0].Cantidad").Should().Be(!esValida);
        }
    }

    public class EnviarOrdenRequestValidatorTests
    {
        private readonly EnviarOrdenRequestValidator _validador = new EnviarOrdenRequestValidator();

        private static EnviarOrdenRequest SolicitudValida()
        {
            return new EnviarOrdenRequest
            {
                OrdenId = Guid.NewGuid(),
                NumeroRastreo = "ABC123456",
                RowVersion = new byte[] { 1 }
            };
        }

        private List<string> Propiedades(EnviarOrdenRequest solicitud)
        {
            return _validador.Validate(solicitud).Errors.Select(e => e.PropertyName).ToList();
        }

        [Fact]
        public void Validate_ConSolicitudCompleta_EsValida()
        {
            _validador.Validate(SolicitudValida()).IsValid.Should().BeTrue();
        }

        [Fact]
        public void OrdenId_VacioNoEsValido()
        {
            var solicitud = SolicitudValida();
            solicitud.OrdenId = Guid.Empty;

            Propiedades(solicitud).Should().Contain("OrdenId");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("     ")]
        public void Rastreo_Obligatorio(string rastreo)
        {
            var solicitud = SolicitudValida();
            solicitud.NumeroRastreo = rastreo;

            Propiedades(solicitud).Should().Contain("NumeroRastreo");
        }

        [Theory]
        [InlineData("ABC12", false)]            // 5 caracteres
        [InlineData("ABC123", true)]            // 6 caracteres
        [InlineData("  abc 123  ", true)]        // normalizado: "ABC123"
        [InlineData("AB#123", false)]           // carácter no permitido
        public void Rastreo_SeValidaYaNormalizadoConLongitudDeSeisACincuenta(string rastreo, bool esValido)
        {
            var solicitud = SolicitudValida();
            solicitud.NumeroRastreo = rastreo;

            Propiedades(solicitud).Contains("NumeroRastreo").Should().Be(!esValido);
        }

        [Fact]
        public void Rastreo_ConMasDeCincuentaCaracteres_NoEsValido()
        {
            var solicitud = SolicitudValida();
            solicitud.NumeroRastreo = new string('A', 51);

            Propiedades(solicitud).Should().Contain("NumeroRastreo");
        }
    }

    public class CambiarEstadoOrdenRequestValidatorTests
    {
        private readonly CambiarEstadoOrdenRequestValidator _validador = new CambiarEstadoOrdenRequestValidator();

        private static CambiarEstadoOrdenRequest SolicitudValida()
        {
            return new CambiarEstadoOrdenRequest
            {
                OrdenId = Guid.NewGuid(),
                EstadoSolicitado = EstadoOrden.Confirmada,
                RowVersion = new byte[] { 1 }
            };
        }

        [Fact]
        public void Validate_ConSolicitudCompleta_EsValida()
        {
            _validador.Validate(SolicitudValida()).IsValid.Should().BeTrue();
        }

        [Fact]
        public void OrdenId_VacioNoEsValido()
        {
            var solicitud = SolicitudValida();
            solicitud.OrdenId = Guid.Empty;

            _validador.Validate(solicitud).Errors.Select(e => e.PropertyName).Should().Contain("OrdenId");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(99)]
        public void Estado_FueraDelEnumNoEsValido(int valor)
        {
            var solicitud = SolicitudValida();
            solicitud.EstadoSolicitado = (EstadoOrden)valor;

            _validador.Validate(solicitud).Errors.Select(e => e.PropertyName).Should().Contain("EstadoSolicitado");
        }

        [Fact]
        public void Estado_DelEnumEsValidoAunqueLaTransicionNoLoSea()
        {
            // La validez de la transición es una regla de dominio/servicio, no de FluentValidation.
            var solicitud = SolicitudValida();
            solicitud.EstadoSolicitado = EstadoOrden.Entregada;

            _validador.Validate(solicitud).IsValid.Should().BeTrue();
        }
    }
}
