using AutoMapper;
using Pedidos.Application;
using Pedidos.Application.Requests;
using Pedidos.Infrastructure;
using Pedidos.Web.Extensions;
using Pedidos.Web.Models;
using Pedidos.Web.ViewModels;

namespace Pedidos.Web.Mappings
{
    /// <summary>
    /// Mapeos de la capa Web (FormModel -> Request, Dto -> ViewModel). Solo copian y convierten datos:
    /// no contienen reglas de negocio (esas viven en Domain/Application).
    /// </summary>
    public class WebMappingProfile : Profile
    {
        public WebMappingProfile()
        {
            // ----- Formularios -> Requests de Application
            CreateMap<DetalleOrdenFormModel, DetalleOrdenRequest>();
            CreateMap<CrearOrdenFormModel, CrearOrdenRequest>();

            CreateMap<EnviarOrdenFormModel, EnviarOrdenRequest>()
                .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => VersionOrden.DesdeTexto(src.RowVersion)));

            CreateMap<CambiarEstadoFormModel, CambiarEstadoOrdenRequest>()
                .ForMember(dest => dest.EstadoSolicitado, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => VersionOrden.DesdeTexto(src.RowVersion)));

            // ----- OrdenDto -> ViewModels
            CreateMap<DetalleOrdenDto, DetalleOrdenViewModel>();

            CreateMap<OrdenDto, OrdenListadoItemViewModel>();

            CreateMap<OrdenDto, OrdenDetalleViewModel>()
                .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => VersionOrden.ATexto(src.RowVersion)));

            CreateMap<OrdenDto, EnviarOrdenFormModel>()
                .ForMember(dest => dest.OrdenId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.NumeroRastreo, opt => opt.Ignore())
                .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => VersionOrden.ATexto(src.RowVersion)));

            CreateMap<OrdenDto, RastreoViewModel>()
                .ForMember(dest => dest.Criterio, opt => opt.Ignore())
                .ForMember(dest => dest.Encontrada, opt => opt.MapFrom(src => true));

            // ----- Usuarios (Identity)
            CreateMap<ApplicationUser, UsuarioListadoViewModel>()
                .ForMember(dest => dest.Rol, opt => opt.Ignore());

            // MemberList.None: solo se copian los miembros indicados; el resto de IdentityUser
            // (Id, PasswordHash, SecurityStamp, etc.) lo administra Identity, nunca el formulario.
            CreateMap<RegistroFormModel, ApplicationUser>(MemberList.None)
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.NombreCompleto, opt => opt.MapFrom(src => src.NombreCompleto));
        }
    }
}
