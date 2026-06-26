using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Entities;

public class UserEntity
{
    public required Guid Id { get; set; } // keycloak user id
    public required string DisplayName { get; set; }
    public required string Email { get; set; }
    public string? AvatarUrl { get; set; } // TODO: using minio to store user avatar images

    public ICollection<LibraryEntity> Libraries { get; set; } = [];
    public ICollection<LoanRequestEntity> Loans { get; set; } = [];
}
