using AutoMapper;
using Pedidos.Domain.Entities;

namespace Pedidos.Application.Mappings
{
    /// <summary>
    /// Mapeos de la capa Application. No contiene reglas de negocio: solo copia/convierte datos.
    /// Las transiciones permitidas las calcula el Domain y las asigna el servicio.
    /// </summary>
    public class ApplicationMappingProfile : Profile
    {
        public ApplicationMappingProfile()
        {
            CreateMap<DetalleOrden, DetalleOrdenDto>();

            // Orden -> OrdenDto
            CreateMap<Orden, OrdenDto>()
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.Estado.ToString()))
                .ForMember(dest => dest.TransicionesPermitidas, opt => opt.Ignore());
        }
    }
}
