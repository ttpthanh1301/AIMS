using AIMS.BackendServer.Data.Entities;
using AIMS.ViewModels.Systems;
using AutoMapper;

namespace AIMS.BackendServer.MappingProfiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Role mappings
        CreateMap<AppRole, RoleVm>();
        CreateMap<CreateRoleRequest, AppRole>();
        CreateMap<UpdateRoleRequest, AppRole>();

        // User mappings
        CreateMap<AppUser, UserVm>()
            .ForMember(dest => dest.FullName,
                opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
        CreateMap<CreateUserRequest, AppUser>();
        // User mappings — thêm vào MappingProfile
        CreateMap<AppUser, UserVm>()
            .ForMember(dest => dest.FullName,
                opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));

        CreateMap<CreateUserRequest, AppUser>()
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.EmailConfirmed,
                opt => opt.MapFrom(_ => true));
        // Function
        CreateMap<Function, FunctionVm>();
        CreateMap<CreateFunctionRequest, Function>();
        CreateMap<UpdateFunctionRequest, Function>();

        // Command
        CreateMap<Command, CommandVm>();
    }

}