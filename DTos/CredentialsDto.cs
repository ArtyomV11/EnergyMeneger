using EnergyMeneger.Models;

namespace EnergyMeneger.DTOs;

public sealed record CredentialsDto(
    UserDto User,
    string InitialPassword);
