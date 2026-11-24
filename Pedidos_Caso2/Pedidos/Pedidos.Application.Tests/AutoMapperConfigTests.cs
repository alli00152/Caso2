using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Pedidos.Application.Mappings;
using Pedidos.Domain.Enums;
using Xunit;

namespace Pedidos.Application.Tests
{
    public class AutoMapperConfigTests
    {
        private static MapperConfiguration CrearConfiguracion()
        {
            return new MapperConfiguration(c => c.AddProfile<ApplicationMappingProfile>(), NullLoggerFactory.Instance);
        }

        [Fact]
        public void ApplicationMappingProfile_TieneUnaConfiguracionValida()
        {
            var configuracion = CrearConfiguracion();

            // Falla si algún miembro de destino quedó sin mapear.
            configuracion.Invoking(c => c.AssertConfigurationIsValid()).Should().NotThrow();
        }

        [Fact]
        public void Map_OrdenAOrdenDto_ConvierteElEstadoATextoYCopiaLosDetalles()
        {
            var mapper = CrearConfiguracion().CreateMapper();
            var orden = OrdenFabrica.Crear(estado: EstadoOrden.Confirmada);

            var dto = mapper.Map<OrdenDto>(orden);

            dto.Id.Should().Be(orden.Id);
            dto.Estado.Should().Be("Confirmada");
            dto.Detalles.Should().HaveCount(1);
            dto.Detalles[0].Producto.Should().Be("Teclado");
        }
    }
}
