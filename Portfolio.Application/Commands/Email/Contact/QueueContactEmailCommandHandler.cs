using AutoMapper;
using MediatR;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Commands.Email.Contact;

/// <summary>
/// Handles Contact Me email requests by mapping the command to the email request model
/// and delegating processing to the email dispatcher.
/// </summary>
public sealed class QueueContactEmailCommandHandler : IRequestHandler<QueueContactEmailCommand>
{
    private readonly IEmailDispatcher _emailDispatcher;
    private readonly IMapper _mapper;

    public QueueContactEmailCommandHandler(IEmailDispatcher emailDispatcher, IMapper mapper)
    {
        _emailDispatcher = emailDispatcher;
        _mapper = mapper;
    }

    public async Task Handle(QueueContactEmailCommand request, CancellationToken cancellationToken)
    {
        var contactEmailRequest = _mapper.Map<ContactEmailRequestDTO>(request);

        await _emailDispatcher.QueueContactEmailAsync(contactEmailRequest, cancellationToken);
    }
}