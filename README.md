# WebSharper.Push

[![NuGet](https://img.shields.io/badge/nuget-not%20published-lightgrey)](https://github.com/dotnet-websharper/PushAPI)

WebSharper bindings for the [Push API](https://developer.mozilla.org/en-US/docs/Web/API/Push_API) —
subscribing to a push service and receiving push messages in a service worker from F# and C# web apps.

The Push API requires a [secure context](https://developer.mozilla.org/en-US/docs/Web/Security/Defenses/Secure_Contexts)
(HTTPS or localhost), a push service (such as FCM or Mozilla autopush) and a registered service worker.

This repository contains:

| Project | Purpose |
| --- | --- |
| `WebSharper.Push` | The binding: WIG definitions for `PushManager`, `PushSubscription`, `PushSubscriptionOptions`, `PushMessageData`, `PushEvent`, and their string enums. |
| `WebSharper.Push.Sample` | An ASP.NET Core SPA sample that reports the browser's push capabilities, registers a service worker and drives the push subscription. |

## Live demos

**Try it live:** [Open the sample in your browser](https://dotnet-websharper.github.io/PushAPI/)

![Push API sample](docs/sample.png)

The sample doubles as the binding's [test case](#running-the-sample): it reads
`PushManager.supportedContentEncodings`, registers a service worker, queries `permissionState` and
`getSubscription`, and attempts `subscribe`. A real push still needs a VAPID key and a server, so the
sample reports the browser's own error instead of faking a subscription.

- **Sample (this repository):** <https://dotnet-websharper.github.io/PushAPI/>
- **MDN Push API:** <https://developer.mozilla.org/en-US/docs/Web/API/Push_API>
- **Binding source:** <https://github.com/dotnet-websharper/PushAPI>
- **WebSharper bindings:** <https://websharper.com/bindings>

## Features

- `PushManager` with `getSubscription`, `permissionState`, `subscribe`, and the static `supportedContentEncodings`.
- `PushSubscription` with `endpoint`, `expirationTime`, `options`, `getKey`, `toJSON`, and `unsubscribe`.
- `PushSubscriptionOptions` (`userVisibleOnly`, `applicationServerKey`).
- `PushMessageData` (`arrayBuffer`, `blob`, `bytes`, `json`, `text`).
- `PushEvent` (inheriting `Dom.Event`) with `data`, plus the `PushEventInit` dictionary.
- `PushPermissionState` and `PushEncryptionKeys` string enums.

## Installation

The binding is not currently published to NuGet. Reference the project from source:

```bash
git clone https://github.com/dotnet-websharper/PushAPI.git
```

```xml
<ItemGroup>
  <ProjectReference Include="path/to/WebSharper.Push/WebSharper.Push.fsproj" />
</ItemGroup>
```

## Usage

```fsharp
namespace WebSharper.Push.Demo

open WebSharper
open WebSharper.JavaScript
open WebSharper.Push

[<JavaScript>]
module Demo =
    /// Subscribe the current service worker registration to the push service.
    let subscribe (registration: obj) =
        promise {
            let manager = registration?pushManager |> As<PushManager>
            let options = PushSubscriptionOptions(UserVisibleOnly = true)
            let! subscription = manager.Subscribe(options)
            return subscription.Endpoint
        }
```

`registration` is a `ServiceWorkerRegistration`; its `pushManager` property exposes the `PushManager`. The
`PushSubscriptionOptions` dictionary is passed to `subscribe()`, and `userVisibleOnly = true` is required by
Chromium-based browsers. Handle `PushEvent` in the service worker, reading the payload through `PushMessageData`:

```fsharp
[<JavaScript>]
module ServiceWorker =
    let onPush (event: PushEvent) =
        let data = event.Data
        if not (Optional.isUndefined data) then
            Console.Log(data.Value.Text())
```

## Running the sample

Build the sample in Release and serve it from the ASP.NET Core host (or any static server):

```bash
dotnet build WebSharper.Push.Sample/WebSharper.Push.Sample.fsproj -c Release
dotnet run --project WebSharper.Push.Sample/WebSharper.Push.Sample.fsproj
```

Open <http://localhost:50560/>. The page reports the browser's supported content encodings, registers
`service-worker.js`, and lets you query the permission state, read the existing subscription and try to subscribe.
The service worker's `push` handler logs incoming payloads and shows a notification.

## Building from source

### Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) or later.

### Build

```bash
dotnet build WebSharper.Push.sln -c Release
```

The binding targets `netstandard2.0`; the sample targets `net10.0`.

## Deployment

Pushing to `main` triggers `.github/workflows/ghpages.yml`, which builds the sample in Release and publishes
`wwwroot` (including `service-worker.js`) to the `gh-pages` branch.

## Repository layout

```
WebSharper.Push/            # binding (WIG definitions)
  Main.fs
  WebSharper.Push.fsproj
  wsconfig.json
WebSharper.Push.Sample/     # ASP.NET Core SPA sample
  Client.fs
  Startup.fs
  wwwroot/
    index.html
    site.css
    service-worker.js
docs/sample.png
.github/workflows/ghpages.yml   # build + deploy the sample to GitHub Pages
README.md
LICENSE.md
```

## Notes on the API

- **Secure context:** the Push API is only exposed in secure contexts. Serve the page over HTTPS or from localhost.
- **Push service:** a push subscription is tied to a browser push service (FCM, Mozilla autopush, and so on) and to a
  server that sends the encrypted messages.
- **Service worker:** `subscribe()` must be called on a service worker registration, and a service worker must handle
  the `push` event. Use `ServiceWorkerRegistration.pushManager` to obtain the `PushManager`.
- **User gesture:** call `subscribe()` from a user gesture (for example, a button click). Browsers may reject the
  request otherwise, and the user is asked for notification permission.
- **`userVisibleOnly`:** pass `PushSubscriptionOptions(UserVisibleOnly = true)` to `subscribe()` and `permissionState()`.
  Chromium rejects a subscription that does not enable it.
- **`applicationServerKey`:** `PushSubscriptionOptions.applicationServerKey` accepts an `ArrayBuffer`, any typed array
  (`ArrayBufferView`), or a base64url-encoded `string` (the VAPID public key).
- **Nullable values:** `PushManager.getSubscription()` resolves to `null` when there is no subscription,
  `PushSubscription.expirationTime` and `PushSubscription.getKey()` may be `null`, and `PushEvent.data` may be `null`.
  The binding models these as `Optional<'T>`; check with `Optional.isUndefined` and `isNull`.
- **`supportedContentEncodings`:** the static `PushManager.supportedContentEncodings` property is a `string[]`.
- **`PushEvent` constructor:** the browser only exposes it inside a service worker, so a page cannot build a
  synthetic event. The sample's `service-worker.js` reads `event.data` in its `push` handler.
- **Browser support:** supported by Chromium-based browsers and Firefox; Safari supports the Push API for web apps
  added to the Home Screen (iOS 16.4+).

## License

Apache 2.0 — see [LICENSE.md](LICENSE.md).
