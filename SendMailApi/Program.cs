using RestSharp;
using RestSharp.Authenticators;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/SendMail", async (Mail Mail) =>
{
    switch (Mail.ClientMail)
    {
        case ClientMail.SendGrid:
            var clientSendGrid = new SendGridClient(Mail.ClientKey);

            var newMail = MailHelper.CreateSingleEmailToMultipleRecipients(Mail.From, Mail.Recipient, Mail.Subject, Mail.ContentText, Mail.ContentHtml);

            var responseSendGrid = await clientSendGrid.SendEmailAsync(newMail);

            if (responseSendGrid.StatusCode == HttpStatusCode.Accepted)
                return Results.Ok(new Return(true, responseSendGrid.Body));
            else
                return Results.BadRequest(new Return(false, responseSendGrid.Body));

        case ClientMail.MailGun:
            var clientMailGun = new RestClient { BaseUrl = new Uri("https://api.mailgun.net/v3"), Authenticator = new HttpBasicAuthenticator("api", Mail.ClientKey) };

            var request = new RestRequest { Resource = "{domain}/messages", Method = Method.POST };

            request.AddParameter("domain", Mail.From.Email.Split('@')[1], ParameterType.UrlSegment);
            request.AddParameter("from", Mail.From.Name + " <" + Mail.From.Email + ">");
            request.AddParameter("subject", Mail.Subject);
            request.AddParameter("text", Mail.ContentText);
            request.AddParameter("html", Mail.ContentHtml);

            foreach (var recipient in Mail.Recipient)
                request.AddParameter("to", recipient.Email);

            var responseMailGun = clientMailGun.Execute(request);

            if (responseMailGun.StatusCode == HttpStatusCode.OK)
                return Results.Ok(new Return(true, responseMailGun.Content));
            else
                return Results.BadRequest(new Return(false, responseMailGun.Content));

        default:
            return Results.BadRequest(new Return(false, "ClientMail is invalid."));
    }
}).WithName("SendMail").Produces<Return>();


app.Run();

internal enum ClientMail { SendGrid = 1, MailGun = 2 };

internal record Mail(ClientMail ClientMail, string ClientKey, EmailAddress From, List<EmailAddress> Recipient, string Subject, string ContentText, string ContentHtml);

internal record Return(bool Sucesso, object Response);