using AutoMapper;
using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Commands.Email.Contact;

/// <summary>
/// Defines AutoMapper mappings for Contact Me email requests.
/// </summary>
public sealed class QueueContactEmailProfile : Profile
{
    public QueueContactEmailProfile()
    {
        CreateMap<QueueContactEmailCommand, ContactEmailRequestDTO>();
    }
}