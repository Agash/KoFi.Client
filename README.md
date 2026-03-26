# KoFi.Client

Modern .NET packages for transport-neutral Ko-fi webhook handling and ASP.NET Core integration.

## Packages

- `KoFi.Client`
- `KoFi.Client.AspNetCore`
- `KoFi.Client.DependencyInjection`

## What this repo provides

- transport-neutral Ko-fi webhook parsing and normalization
- ASP.NET Core endpoint mapping helpers
- dependency-injection registration helpers
- an interactive Spectre.Console sample that can expose a local endpoint through Azure Dev Tunnels

## Install

```bash
dotnet add package KoFi.Client
dotnet add package KoFi.Client.AspNetCore
```

Add the DI helpers when you want registration extensions:

```bash
dotnet add package KoFi.Client.DependencyInjection
```

## Quick start

```csharp
using KoFi.Client.Webhooks;

KoFiWebhookHandler handler = new();
WebhookHandleResult result = await handler.HandleAsync(request, cancellationToken);

if (result.IsAccepted)
{
    Console.WriteLine("Ko-fi webhook accepted.");
}
```

## ASP.NET Core webhook example

```csharp
using KoFi.Client.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapKoFiWebhook(
    "/webhooks/kofi",
    static (_, _) => Task.FromResult(new KoFiWebhookOptions()),
    static (evt, _, _) =>
    {
        Console.WriteLine(evt.Type);
        return Task.CompletedTask;
    });

await app.RunAsync();
```

## Sample

Run the interactive sample to host a local Ko-fi webhook endpoint and optionally expose it publicly through Azure Dev Tunnels:

```bash
dotnet run --project samples/KoFi.Client.Sample
```

See [samples/KoFi.Client.Sample/README.md](/C:/repos/StreamWeaver/external/KoFi.Client/samples/KoFi.Client.Sample/README.md) for the walkthrough.

## Development

```bash
dotnet test KoFi.Client.slnx
```

