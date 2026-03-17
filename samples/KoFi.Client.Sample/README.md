# KoFi.Client.Sample

Interactive console sample for:

- `KoFi.Client`
- `KoFi.Client.AspNetCore`
- `DevTunnels.Client`

This sample:

1. hosts a local ASP.NET Core endpoint,
2. maps a Ko-fi webhook route,
3. optionally starts an Azure Dev Tunnel,
4. prints normalized Ko-fi events live in the console.

## What it demonstrates

- transport-neutral webhook handling through `KoFi.Client`
- ASP.NET Core hosting through `KoFi.Client.AspNetCore`
- public HTTPS exposure through `DevTunnels.Client`
- interactive setup flow through `Spectre.Console`

## Run

```bash
dotnet run --project external/KoFi.Client/samples/KoFi.Client.Sample