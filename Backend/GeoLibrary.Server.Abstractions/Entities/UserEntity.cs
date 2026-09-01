using System;
using System.Collections.Generic;
using System.Text;

namespace GeoLibrary.Server.Abstractions.Entities;

public class UserEntity
{
    public required Guid Id { get; set; } // keycloak user id
    public required string DisplayName { get; set; }
    public required string Email { get; set; }
    /// <summary>
    /// Chiave dell'oggetto su MinIO, non un URL: i link di download sono firmati
    /// e scadono dopo un'ora, quindi vengono rigenerati a ogni lettura del profilo.
    /// </summary>
    public string? AvatarKey { get; set; }

    public ICollection<LibraryEntity> Libraries { get; set; } = [];
    public ICollection<LoanRequestEntity> Loans { get; set; } = [];
}
