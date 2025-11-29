namespace Application.Mappers;

using AutoMapper;
using Domain.Entities;
using Application.DTOs;

/// <summary>
/// AutoMapper profile for mapping between domain and application layers.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.MapFrom(src => src.CreatedAtUtc))
            .ForMember(dest => dest.UpdatedAtUtc, opt => opt.MapFrom(src => src.UpdatedAtUtc));
    }
}
