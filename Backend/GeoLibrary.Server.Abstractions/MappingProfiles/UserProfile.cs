using AutoMapper;
using GeoLibrary.Server.Abstractions.Dtos.User;
using GeoLibrary.Server.Abstractions.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.MappingProfiles;

public class UserProfile : Profile
{

    public UserProfile()
    {
        CreateMap<UserEntity, ProfileDto>();
    }

}
