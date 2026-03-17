namespace KoFi.Client.Options;

/// <summary>
/// Represents the options required to validate and process a Ko-fi webhook request.
/// </summary>
public sealed class KoFiWebhookOptions
{
    /// <summary>
    /// Gets or sets the Ko-fi verification token configured for the webhook endpoint.
    /// </summary>
    /// <remarks>
    /// Ko-fi includes the verification token inside the posted payload rather than using
    /// an HMAC signature header. The incoming token must match this configured value for
    /// the webhook to be accepted.
    /// </remarks>
    public required string VerificationToken { get; init; }
}