using AutoMapper;
using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Commands.Email.ContactMe;

/// <summary>
/// Defines AutoMapper mappings for Contact Me email requests.
/// </summary>
public sealed class QueueContactMeProfile : Profile
{
    public QueueContactMeProfile()
    {
        CreateMap<QueueContactMeCommand, ContactMeRequestDTO>();
    }
}