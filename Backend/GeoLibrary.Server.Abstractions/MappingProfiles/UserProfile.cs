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
        // Gli URL dell'avatar non stanno in tabella (c'e' solo la chiave):
        // li costruisce il controller firmandoli a ogni richiesta.
        CreateMap<UserEntity, ProfileDto>()
            .ForMember(d => d.AvatarUrl, opt => opt.Ignore())
            .ForMember(d => d.AvatarThumbnailUrl, opt => opt.Ignore());
    }

}
