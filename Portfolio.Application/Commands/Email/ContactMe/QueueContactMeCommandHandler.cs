using AutoMapper;
using MediatR;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Commands.Email.ContactMe;

/// <summary>
/// Handles Contact Me email requests by mapping the command to the email request model
/// and delegating processing to the email dispatcher.
/// </summary>
public sealed class QueueContactMeCommandHandler : IRequestHandler<QueueContactMeCommand>
{
    private readonly IEmailDispatcher _emailDispatcher;
    private readonly IMapper _mapper;

    public QueueContactMeCommandHandler(IEmailDispatcher emailDispatcher, IMapper mapper)
    {
        _emailDispatcher = emailDispatcher;
        _mapper = mapper;
    }

    public async Task Handle(QueueContactMeCommand request, CancellationToken cancellationToken)
    {
        var contactMeRequest = _mapper.Map<ContactMeRequestDTO>(request);

        await _emailDispatcher.QueueContactMeEmailAsync(contactMeRequest, cancellationToken);
    }
}