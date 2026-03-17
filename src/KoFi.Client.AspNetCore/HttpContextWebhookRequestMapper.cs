using Agash.Webhook.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace KoFi.Client.AspNetCore;

/// <summary>
/// Provides helper methods for converting ASP.NET Core <see cref="HttpContext"/> instances
/// into transport-neutral <see cref="WebhookRequest"/> objects.
/// </summary>
public static class HttpContextWebhookRequestMapper
{
    /// <summary>
    /// Creates a <see cref="WebhookRequest"/> from the specified <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="context">The HTTP context to convert.</param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to observe cancellation while reading the body.
    /// </param>
    /// <returns>
    /// A task that completes with a fully populated transport-neutral webhook request.
    /// </returns>
    public static async Task<WebhookRequest> FromHttpContextAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        Dictionary<string, string[]> headers = new(StringComparer.OrdinalIgnoreCase);

        foreach ((string key, StringValues value) in context.Request.Headers)
        {
            headers[key] = [.. value.Select(static x => x ?? string.Empty)];
        }

        byte[] body;

        if (context.Request.Body.CanSeek)
        {
            context.Request.Body.Position = 0;
        }

        using (MemoryStream buffer = new())
        {
            await context.Request.Body.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            body = buffer.ToArray();
        }

        if (context.Request.Body.CanSeek)
        {
            context.Request.Body.Position = 0;
        }

        return new WebhookRequest
        {
            Method = context.Request.Method,
            Path = context.Request.Path.HasValue ? context.Request.Path.Value! : "/",
            QueryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
            ContentType = context.Request.ContentType,
            Headers = headers,
            Body = body,
        };
    }
}