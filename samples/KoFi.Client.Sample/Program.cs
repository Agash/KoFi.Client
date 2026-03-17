using DevTunnels.Client;
using KoFi.Client.AspNetCore;
using KoFi.Client.DependencyInjection;
using KoFi.Client.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Spectre.Console;
using System.Collections.Concurrent;

CancellationTokenSource shutdown = new();

Console.CancelKeyPress += static (_, e) =>
{
    e.Cancel = true;
};

try
{
    await SampleApplication.RunAsync(shutdown.Token).ConfigureAwait(false);
}
catch (OperationCanceledException)
{
    // Normal shutdown path.
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
    Environment.ExitCode = 1;
}

internal static class SampleApplication
{
    /// <summary>
    /// Runs the Ko-fi sample walkthrough.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to observe shutdown requests.
    /// </param>
    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.Clear();

        AnsiConsole.Write(
            new FigletText("KoFi Sample")
                .Color(Color.HotPink));

        AnsiConsole.MarkupLine("[grey]Transport-neutral Ko-fi webhook sample with ASP.NET Core, Spectre.Console, and DevTunnels.Client.[/]");
        AnsiConsole.WriteLine();

        SampleConfiguration configuration = PromptConfiguration();

        ConcurrentQueue<KoFiWebhookEvent> receivedEvents = new();
        object consoleLock = new();

        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        _ = builder.WebHost.UseUrls($"http://127.0.0.1:{configuration.LocalPort}");
        _ = builder.Services.AddKoFiClient();

        WebApplication app = builder.Build();

        _ = app.MapGet(
            "/",
            () => Results.Text(
                "KoFi.Client.Sample is running.\n" +
                "POST Ko-fi webhook payloads to the configured route.\n",
                "text/plain"));

        _ = app.MapKoFiWebhook(
            configuration.WebhookPath,
            configuration.VerificationToken,
            async (evt, _, _) =>
            {
                receivedEvents.Enqueue(evt);

                lock (consoleLock)
                {
                    RenderReceivedEvent(evt);
                }

                await Task.CompletedTask.ConfigureAwait(false);
            },
            async (result, httpContext, _) =>
            {
                lock (consoleLock)
                {
                    string remoteIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    string requestId = httpContext.TraceIdentifier;

                    string auth = result.IsAuthenticated ? "[green]yes[/]" : "[red]no[/]";
                    string known = result.IsKnownEvent ? "[green]yes[/]" : "[yellow]no[/]";
                    string status = $"[blue]{result.Response.StatusCode}[/]";

                    AnsiConsole.MarkupLineInterpolated(
                        $"[grey]Request[/] [white]{Markup.Escape(requestId)}[/] from [white]{Markup.Escape(remoteIp)}[/] -> status {status}, authenticated {auth}, known event {known}.");

                    if (!string.IsNullOrWhiteSpace(result.FailureReason))
                    {
                        AnsiConsole.MarkupLineInterpolated($"[yellow]Reason:[/] {Markup.Escape(result.FailureReason)}");
                    }
                }

                await Task.CompletedTask.ConfigureAwait(false);
            });

        string localBaseUrl = $"http://127.0.0.1:{configuration.LocalPort}";

        await app.StartAsync(cancellationToken).ConfigureAwait(false);

        RenderStartupSummary(configuration, localBaseUrl);

        DevTunnelsRuntime? devTunnelsRuntime = null;

        if (configuration.UseDevTunnels)
        {
            devTunnelsRuntime = await StartDevTunnelsAsync(configuration, cancellationToken).ConfigureAwait(false);
            RenderTunnelSummary(configuration, devTunnelsRuntime.PublicBaseUrl);
        }

        RenderUsageInstructions(configuration, localBaseUrl, devTunnelsRuntime?.PublicBaseUrl);

        await RunCommandLoopAsync(configuration, receivedEvents, devTunnelsRuntime, consoleLock, cancellationToken)
            .ConfigureAwait(false);

        if (devTunnelsRuntime is not null)
        {
            await devTunnelsRuntime.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }

        await app.StopAsync(CancellationToken.None).ConfigureAwait(false);
        await app.DisposeAsync().ConfigureAwait(false);
    }

    private static SampleConfiguration PromptConfiguration()
    {
        int localPort = AnsiConsole.Prompt(
            new TextPrompt<int>("Local [green]HTTP port[/]?")
                .DefaultValue(5073)
                .ValidationErrorMessage("[red]Please enter a valid port.[/]")
                .Validate(port => port is > 0 and <= 65535
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]Port must be between 1 and 65535.[/]")));

        string webhookPath = AnsiConsole.Prompt(
            new TextPrompt<string>("Webhook [green]path[/]?")
                .DefaultValue("/webhooks/kofi/events")
                .AllowEmpty());

        if (string.IsNullOrWhiteSpace(webhookPath))
        {
            webhookPath = "/webhooks/kofi/events";
        }

        if (!webhookPath.StartsWith('/'))
        {
            webhookPath = "/" + webhookPath;
        }

        string verificationToken = AnsiConsole.Prompt(
            new TextPrompt<string>("Ko-fi [green]verification token[/]?")
                .PromptStyle("hotpink")
                .Secret());

        bool useDevTunnels = AnsiConsole.Confirm("Use [green]Azure Dev Tunnels[/] for a public HTTPS URL?", true);

        string tunnelId = "kofi-client-sample";
        LoginProvider loginProvider = LoginProvider.GitHub;

        if (useDevTunnels)
        {
            tunnelId = AnsiConsole.Prompt(
                new TextPrompt<string>("Dev Tunnel [green]tunnel ID[/]?")
                    .DefaultValue("kofi-client-sample")
                    .AllowEmpty());

            if (string.IsNullOrWhiteSpace(tunnelId))
            {
                tunnelId = "kofi-client-sample";
            }

            loginProvider = AnsiConsole.Prompt(
                new SelectionPrompt<LoginProvider>()
                    .Title("Login provider for [green]devtunnel[/]?")
                    .AddChoices(LoginProvider.GitHub, LoginProvider.Microsoft));
        }

        return new SampleConfiguration(
            LocalPort: localPort,
            WebhookPath: webhookPath,
            VerificationToken: verificationToken,
            UseDevTunnels: useDevTunnels,
            TunnelId: tunnelId,
            LoginProvider: loginProvider);
    }

    private static async Task<DevTunnelsRuntime> StartDevTunnelsAsync(
        SampleConfiguration configuration,
        CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Azure Dev Tunnels walkthrough[/]");
        AnsiConsole.WriteLine();

        DevTunnelsClient client = new(new DevTunnelsClientOptions
        {
            CommandTimeout = TimeSpan.FromSeconds(20),
        });

        DevTunnelCliProbeResult probe = await client.ProbeCliAsync(cancellationToken).ConfigureAwait(false);

        if (!probe.IsInstalled)
        {
            throw new InvalidOperationException(
                "The devtunnel CLI is not installed or could not be found. " +
                "Install it first, then re-run the sample.");
        }

        AnsiConsole.MarkupLineInterpolated($"[green]CLI found:[/] devtunnel [white]{Markup.Escape(probe.Version?.ToString() ?? "unknown")}[/]");

        AnsiConsole.MarkupLineInterpolated(
            $"[grey]Ensuring login with[/] [white]{Markup.Escape(configuration.LoginProvider.ToString())}[/][grey]...[/]");

        _ = await client.EnsureLoggedInAsync(configuration.LoginProvider, cancellationToken).ConfigureAwait(false);

        AnsiConsole.MarkupLine("[green]Login confirmed.[/]");

        _ = await client.CreateOrUpdateTunnelAsync(
            configuration.TunnelId,
            new DevTunnelOptions
            {
                Description = "KoFi.Client.Sample tunnel",
                AllowAnonymous = true,
            },
            cancellationToken).ConfigureAwait(false);

        _ = await client.CreateOrReplacePortAsync(
            configuration.TunnelId,
            configuration.LocalPort,
            new DevTunnelPortOptions
            {
                Protocol = "http",
            },
            cancellationToken).ConfigureAwait(false);

        IDevTunnelHostSession session = await client.StartHostSessionAsync(
            new DevTunnelHostStartOptions
            {
                TunnelId = configuration.TunnelId,
            },
            cancellationToken).ConfigureAwait(false);

        await session.WaitForReadyAsync(cancellationToken).ConfigureAwait(false);

        Uri publicBaseUrl = session.PublicUrl
            ?? throw new InvalidOperationException("The Dev Tunnel host session became ready without a public URL.");

        AnsiConsole.MarkupLineInterpolated($"[green]Tunnel ready:[/] [link]{Markup.Escape(publicBaseUrl.ToString())}[/]");

        return new DevTunnelsRuntime(session, publicBaseUrl);
    }

    private static void RenderStartupSummary(SampleConfiguration configuration, string localBaseUrl)
    {
        string localWebhookUrl = CombineUrl(localBaseUrl, configuration.WebhookPath);

        Table table = new Table()
            .RoundedBorder()
            .BorderColor(Color.HotPink)
            .AddColumn("[bold]Setting[/]")
            .AddColumn("[bold]Value[/]");

        _ = table.AddRow("Local base URL", $"[white]{Markup.Escape(localBaseUrl)}[/]");
        _ = table.AddRow("Webhook path", $"[white]{Markup.Escape(configuration.WebhookPath)}[/]");
        _ = table.AddRow("Local webhook URL", $"[white]{Markup.Escape(localWebhookUrl)}[/]");
        _ = table.AddRow("Verification token", "[grey](hidden)[/]");
        _ = table.AddRow("Dev Tunnels enabled", configuration.UseDevTunnels ? "[green]yes[/]" : "[yellow]no[/]");

        AnsiConsole.Write(new Panel(table)
            .Header("[bold]Local runtime[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.HotPink));
    }

    private static void RenderTunnelSummary(SampleConfiguration configuration, Uri publicBaseUrl)
    {
        string publicWebhookUrl = CombineUrl(publicBaseUrl.ToString().TrimEnd('/'), configuration.WebhookPath);

        Table table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Green)
            .AddColumn("[bold]Setting[/]")
            .AddColumn("[bold]Value[/]");

        _ = table.AddRow("Tunnel ID", $"[white]{Markup.Escape(configuration.TunnelId)}[/]");
        _ = table.AddRow("Public base URL", $"[white]{Markup.Escape(publicBaseUrl.ToString())}[/]");
        _ = table.AddRow("Public webhook URL", $"[white]{Markup.Escape(publicWebhookUrl)}[/]");

        AnsiConsole.Write(new Panel(table)
            .Header("[bold]Public tunnel[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green));
    }

    private static void RenderUsageInstructions(
        SampleConfiguration configuration,
        string localBaseUrl,
        Uri? publicBaseUrl)
    {
        string localWebhookUrl = CombineUrl(localBaseUrl, configuration.WebhookPath);
        string? publicWebhookUrl = publicBaseUrl is null
            ? null
            : CombineUrl(publicBaseUrl.ToString().TrimEnd('/'), configuration.WebhookPath);

        Rows rows = new(
            new Markup("[bold]Walkthrough[/]"),
            new Text(string.Empty),
            new Markup("1. Start this sample and keep it running."),
            new Markup("2. Copy the webhook URL into your Ko-fi dashboard."),
            new Markup("3. Use the exact verification token you entered for this sample."),
            new Markup("4. Trigger a live or test webhook from Ko-fi."),
            new Text(string.Empty),
            new Markup($"[grey]Local webhook URL:[/] [white]{Markup.Escape(localWebhookUrl)}[/]"),
            publicWebhookUrl is not null
                ? new Markup($"[grey]Public webhook URL:[/] [white]{Markup.Escape(publicWebhookUrl)}[/]")
                : new Markup("[grey]Public webhook URL:[/] [yellow](Dev Tunnels disabled)[/]"),
            new Text(string.Empty),
            new Markup("[grey]Commands are available below while the sample is running.[/]"));

        AnsiConsole.Write(new Panel(rows)
            .Header("[bold]How to use the sample[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue));
    }

    private static async Task RunCommandLoopAsync(
        SampleConfiguration configuration,
        ConcurrentQueue<KoFiWebhookEvent> receivedEvents,
        DevTunnelsRuntime? devTunnelsRuntime,
        object consoleLock,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.WriteLine();

            string command = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Choose an action[/]")
                    .AddChoices(
                        "Show webhook URLs",
                        "Show recent events",
                        "Show sample payload hint",
                        "Exit"));

            switch (command)
            {
                case "Show webhook URLs":
                    lock (consoleLock)
                    {
                        string localBaseUrl = $"http://127.0.0.1:{configuration.LocalPort}";
                        string localWebhookUrl = CombineUrl(localBaseUrl, configuration.WebhookPath);

                        Table table = new Table()
                            .RoundedBorder()
                            .AddColumn("[bold]Endpoint[/]")
                            .AddColumn("[bold]URL[/]");

                        _ = table.AddRow("Local", $"[white]{Markup.Escape(localWebhookUrl)}[/]");

                        if (devTunnelsRuntime is not null)
                        {
                            string publicWebhookUrl = CombineUrl(
                                devTunnelsRuntime.PublicBaseUrl.ToString().TrimEnd('/'),
                                configuration.WebhookPath);

                            _ = table.AddRow("Public", $"[white]{Markup.Escape(publicWebhookUrl)}[/]");
                        }

                        AnsiConsole.Write(table);
                    }

                    break;

                case "Show recent events":
                    lock (consoleLock)
                    {
                        if (receivedEvents.IsEmpty)
                        {
                            AnsiConsole.MarkupLine("[yellow]No events have been received yet.[/]");
                            break;
                        }

                        KoFiWebhookEvent[] snapshot = [.. receivedEvents];

                        Table table = new Table()
                            .RoundedBorder()
                            .AddColumn("[bold]Type[/]")
                            .AddColumn("[bold]From[/]")
                            .AddColumn("[bold]Amount[/]")
                            .AddColumn("[bold]Currency[/]")
                            .AddColumn("[bold]Timestamp[/]");

                        foreach (KoFiWebhookEvent evt in snapshot.TakeLast(20))
                        {
                            _ = table.AddRow(
                                Markup.Escape(GetEventDisplayName(evt)),
                                Markup.Escape(evt.FromName ?? "(anonymous)"),
                                evt.Amount.ToString("0.00"),
                                Markup.Escape(evt.Currency),
                                Markup.Escape(evt.Timestamp.ToString("u")));
                        }

                        AnsiConsole.Write(table);
                    }

                    break;

                case "Show sample payload hint":
                    lock (consoleLock)
                    {
                        AnsiConsole.Write(new Panel(GetSamplePayloadHint(configuration.VerificationToken))
                            .Header("[bold]Example Ko-fi form body[/]")
                            .Border(BoxBorder.Rounded)
                            .BorderColor(Color.Yellow));
                    }

                    break;

                case "Exit":
                    return;
            }

            await Task.Yield();
        }
    }

    private static void RenderReceivedEvent(KoFiWebhookEvent evt)
    {
        Grid grid = new();
        _ = grid.AddColumn();
        _ = grid.AddColumn();

        _ = grid.AddRow("[bold]Type[/]", Markup.Escape(GetEventDisplayName(evt)));
        _ = grid.AddRow("[bold]From[/]", Markup.Escape(evt.FromName ?? "(anonymous)"));
        _ = grid.AddRow("[bold]Amount[/]", $"{evt.Amount:0.00}");
        _ = grid.AddRow("[bold]Currency[/]", Markup.Escape(evt.Currency));
        _ = grid.AddRow("[bold]Timestamp[/]", Markup.Escape(evt.Timestamp.ToString("u")));
        _ = grid.AddRow("[bold]Transaction[/]", Markup.Escape(evt.KofiTransactionId ?? "(none)"));

        if (!string.IsNullOrWhiteSpace(evt.Message))
        {
            _ = grid.AddRow("[bold]Message[/]", Markup.Escape(evt.Message));
        }

        AnsiConsole.Write(new Panel(grid)
            .Header("[bold green]Webhook event received[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green));
    }

    private static string GetEventDisplayName(KoFiWebhookEvent evt)
    {
        return evt switch
        {
            KoFiDonationEvent => "Donation",
            KoFiSubscriptionStartedEvent => "Subscription started",
            KoFiSubscriptionRenewedEvent => "Subscription renewed",
            KoFiReferralEvent => "Referral",
            KoFiShopOrderEvent => "Shop order",
            KoFiCommissionEvent => "Commission",
            _ => "Unknown",
        };
    }

    private static string CombineUrl(string baseUrl, string path)
    {
        string normalizedBase = baseUrl.TrimEnd('/');
        string normalizedPath = path.StartsWith('/') ? path : "/" + path;
        return normalizedBase + normalizedPath;
    }

    private static string GetSamplePayloadHint(string verificationToken)
    {
        return """
        Ko-fi posts application/x-www-form-urlencoded with a single field named data.

        Example form field value:
        data={"verification_token":"REPLACE_TOKEN","message_id":"sample-message-id","timestamp":"2026-03-17T12:00:00Z","type":"Donation","is_public":true,"from_name":"Sample Supporter","message":"Hello from the sample","amount":"5.00","url":"https://ko-fi.com","email":"supporter@example.com","currency":"EUR","is_subscription_payment":false,"is_first_subscription_payment":false,"kofi_transaction_id":"txn_123","tier_name":null,"discord_username":null,"discord_user_id":null}

        Replace REPLACE_TOKEN with the configured verification token:
        """
        + Environment.NewLine + verificationToken;
    }

    private sealed record SampleConfiguration(
        int LocalPort,
        string WebhookPath,
        string VerificationToken,
        bool UseDevTunnels,
        string TunnelId,
        LoginProvider LoginProvider);

    /// <summary>
    /// Initializes a new instance of the <see cref="DevTunnelsRuntime"/> class.
    /// </summary>
    /// <param name="session">The live host session returned by the client.</param>
    /// <param name="publicBaseUrl">The public base URL exposed by the tunnel.</param>
    private sealed class DevTunnelsRuntime(
        dynamic session,
        Uri publicBaseUrl)
    {

        /// <summary>
        /// Gets the active host session.
        /// </summary>
        public dynamic Session { get; } = session;

        /// <summary>
        /// Gets the public base URL exposed by the tunnel.
        /// </summary>
        public Uri PublicBaseUrl { get; } = publicBaseUrl;

        /// <summary>
        /// Stops the running tunnel session.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to observe cancellation.
        /// </param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Session.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Best-effort shutdown for the sample.
            }
        }
    }
}