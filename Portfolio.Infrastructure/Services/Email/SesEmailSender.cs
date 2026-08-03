using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Logging;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Infra.AWS.Configuration;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Sends composed email messages through Amazon SES using raw MIME content.
/// </summary>
public sealed class SesEmailSender : IEmailSender
{
    private readonly IAmazonSimpleEmailServiceV2 _ses;
    private readonly SesOptions _options;
    private readonly ILogger<SesEmailSender> _logger;
    private readonly IEmailResourceResolver _resourceResolver;

    public SesEmailSender(
        IAmazonSimpleEmailServiceV2 ses,
        IOptions<SesOptions> options,
        ILogger<SesEmailSender> logger,
        IEmailResourceResolver resourceResolver)
    {
        _ses = ses;
        _options = options.Value;
        _logger = logger;
        _resourceResolver = resourceResolver;
    }

    /// <summary>
    /// Creates a MIME email message and sends it through Amazon SES.
    /// </summary>
    public async Task<string> SendAsync(
        EmailMessage message,
        string htmlBody,
        string textBody,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var mimeMessage = new MimeMessage();

        mimeMessage.From.Add(MailboxAddress.Parse(_options.FromEmail));
        mimeMessage.To.Add(MailboxAddress.Parse(message.RecipientEmail.Address));
        mimeMessage.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = textBody
        };

        foreach (var resource in message.Resources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var content = await _resourceResolver.ResolveAsync(
                resource,
                cancellationToken);

            if (content.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Email resource '{resource.FileName}' resolved to an empty file.");
            }

            var contentTypeParts =
                resource.ContentType.Split(
                    '/',
                    2,
                    StringSplitOptions.RemoveEmptyEntries);

            if (contentTypeParts.Length != 2)
            {
                throw new InvalidOperationException(
                    $"Invalid email resource content type '{resource.ContentType}'.");
            }

            var mimePart = new MimePart(
                contentTypeParts[0],
                contentTypeParts[1])
            {
                Content = new MimeContent(new MemoryStream(content)),
                FileName = resource.FileName,
                ContentTransferEncoding = ContentEncoding.Base64
            };

            if (resource.IsInline)
            {
                AddInlineResource(
                    bodyBuilder,
                    mimePart,
                    resource);
            }
            else
            {
                AddAttachment(
                    bodyBuilder,
                    mimePart,
                    resource);
            }
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        using var memoryStream = new MemoryStream();

        await mimeMessage.WriteToAsync(
            memoryStream,
            cancellationToken);

        _logger.LogInformation(
            "{EventName} | Email MIME created. SizeBytes={SizeBytes}, ResourceCount={ResourceCount}, InlineResourceCount={InlineResourceCount}",
            EmailLogEvents.Ses.EmailMimeCreated,
            memoryStream.Length,
            message.Resources.Count,
            message.Resources.Count(x => x.IsInline));


        // SES must receive the stream from the beginning.
        memoryStream.Position = 0;


        var request = new SendEmailRequest
        {
            FromEmailAddress = _options.FromEmail,
            Content = new Amazon.SimpleEmailV2.Model.EmailContent
            {
                Raw = new RawMessage
                {
                    Data = memoryStream
                }
            }
        };

        _logger.LogInformation(
            "{EventName} | Sending raw MIME email through SES. Recipient={Recipient}, TemplateType={TemplateType}, MimeSizeBytes={MimeSizeBytes}",
            EmailLogEvents.Ses.SesSendStarted,
            message.RecipientEmail.Address,
            message.TemplateType,
            memoryStream.Length);

        try
        {
            var response = await _ses.SendEmailAsync(
                request,
                cancellationToken);

            _logger.LogInformation(
                "{EventName} | SES accepted email. MessageId={MessageId}",
                EmailLogEvents.Ses.EmailAccepted,
                response.MessageId);

            return response.MessageId;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "{EventName} | SES email send cancelled.",
                EmailLogEvents.Ses.SendCancelled);

            throw;
        }
        catch (AmazonSimpleEmailServiceV2Exception ex)
        {
            _logger.LogError(
                ex,
                "{EventName} | SES rejected email. ErrorCode={ErrorCode}",
                EmailLogEvents.Ses.SendFailed,
                ex.ErrorCode);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "{EventName} | Unexpected SES failure.",
                EmailLogEvents.Ses.SendFailed);

            throw;
        }
    }

    private static void AddInlineResource(
        BodyBuilder bodyBuilder,
        MimePart mimePart,
        EmailResource resource)
    {
        if (string.IsNullOrWhiteSpace(resource.ContentId))
        {
            throw new InvalidOperationException(
                $"Inline email resource '{resource.FileName}' requires a ContentId.");
        }

        mimePart.ContentId = resource.ContentId;

        mimePart.ContentDisposition =
            new ContentDisposition(ContentDisposition.Inline)
            {
                FileName = resource.FileName
            };

        bodyBuilder.LinkedResources.Add(mimePart);
    }

    private static void AddAttachment(
        BodyBuilder bodyBuilder,
        MimePart mimePart,
        EmailResource resource)
    {
        mimePart.ContentDisposition =
            new ContentDisposition(ContentDisposition.Attachment)
            {
                FileName = resource.FileName
            };

        bodyBuilder.Attachments.Add(mimePart);
    }
}