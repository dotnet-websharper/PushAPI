namespace WebSharper.Push.Sample

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Notation
open WebSharper.UI.Client
open WebSharper.UI.Templating
open WebSharper.Push

// ---------------------------------------------------------------------------
// The service worker is written in F# as well.
//
// The Worker constructor compiles its entry point into a standalone script
// (WebSharper.Push.Sample.service-worker.js) and the browser runs it. That
// script is exactly what a service worker needs, so we do not keep the
// dedicated worker: we hand the generated script to navigator.serviceWorker.
//
// The service worker global scope is not modelled by core, so the APIs that
// only exist there (registration, clients) are reached through inline
// accessors. The push payload itself is read through the binding's PushEvent
// and PushMessageData types.
// ---------------------------------------------------------------------------
[<JavaScript>]
module ServiceWorker =
    [<Inline "$self.skipWaiting()">]
    let skipWaiting (self: obj) : Promise<unit> = X<Promise<unit>>

    [<Inline "$self.clients.claim()">]
    let claimClients (self: obj) : Promise<unit> = X<Promise<unit>>

    [<Inline "$self.addEventListener('install', $handler)">]
    let onInstall (self: obj) (handler: obj -> unit) : unit = X<unit>

    [<Inline "$self.addEventListener('activate', $handler)">]
    let onActivate (self: obj) (handler: obj -> unit) : unit = X<unit>

    [<Inline "$self.addEventListener('push', $handler)">]
    let onPush (self: obj) (handler: PushEvent -> unit) : unit = X<unit>

    [<Inline "$self.addEventListener('notificationclick', $handler)">]
    let onNotificationClick (self: obj) (handler: obj -> unit) : unit = X<unit>

    [<Inline "$self.registration.showNotification($title, { body: $body, tag: 'websharper-push-demo' })">]
    let showNotification (self: obj) (title: string) (body: string) : Promise<unit> = X<Promise<unit>>

    [<Inline "$event.waitUntil($promise)">]
    let waitUntil (event: obj) (promise: obj) : unit = X<unit>

    [<Inline "$event.notification.close()">]
    let closeNotification (event: obj) : unit = X<unit>

    [<Inline "$self.clients.openWindow($url)">]
    let openWindow (self: obj) (url: string) : Promise<obj> = X<Promise<obj>>

    /// Reads the text payload of a push event through the binding's PushMessageData.
    let payloadText (event: PushEvent) =
        let data = event.Data
        if Optional.isDefined data then data.Value.Text() else "No payload"

    let EntryPoint (self: DedicatedWorkerGlobalScope) =
        let scope = box self
        onInstall scope (fun _ -> skipWaiting scope |> ignore)
        onActivate scope (fun _ -> claimClients scope |> ignore)
        onPush scope (fun event ->
            let body = payloadText event
            Console.Log("Push event received: " + body)
            waitUntil (box event) (showNotification scope "Push received" body))
        onNotificationClick scope (fun event ->
            closeNotification event
            waitUntil event (openWindow scope "../../"))

[<JavaScript>]
module Client =
    // The template is loaded from the DOM, so editing index.html and refreshing
    // the browser is enough; a rebuild is only needed when holes change.
    type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument>

    let status = Var.Create "Not started."
    let encodings = Var.Create "—"
    let subscription = Var.Create "No subscription yet."
    let payload = Var.Create "No payload yet."

    // `navigator.serviceWorker` is not modelled by core's Navigator, so reach it
    // through inline accessors. The Push API hangs off the service worker registration.
    [<Inline "$navigator.serviceWorker">]
    let serviceWorker (navigator: obj) : obj = X<obj>

    // The generated script lives in Content/workers, so its scope is that
    // directory. The registration returned by register() is the one we use: the
    // page itself is not controlled by the worker, but the Push API only needs
    // the registration, not page control.
    [<Inline "$sw.register($path, { type: 'module' })">]
    let register (sw: obj) (path: string) : Promise<obj> = X<Promise<obj>>

    [<Inline "$reg.pushManager">]
    let pushManager (reg: obj) : PushManager = X<PushManager>

    [<Inline "typeof globalThis.PushEvent">]
    let pushEventExposed () : string = X<string>

    let mutable registration : obj = null

    let describeEncodings () =
        let encodings = PushManager.SupportedContentEncodings
        if isNull encodings || encodings.Length = 0 then "None reported."
        else String.concat ", " encodings

    let registerWorker () =
        promise {
            try
                // The Worker constructor compiles the service worker's entry
                // point into a standalone script. We only need that script, so
                // the dedicated worker is terminated straight away.
                let worker = new Worker("service-worker", fun self -> ServiceWorker.EntryPoint self)
                worker.Terminate()
                let sw = serviceWorker JS.Window.Navigator
                let! reg = register sw "Content/workers/WebSharper.Push.Sample.service-worker.js"
                registration <- reg
                status := "Service worker registered."
            with e ->
                status := $"Service worker registration failed: {e.Message}"
        }

    let checkPermission () =
        promise {
            try
                if isNull registration then
                    status := "No service worker registration yet."
                else
                    let manager = pushManager registration
                    let options = new PushSubscriptionOptions(UserVisibleOnly = true)
                    let! state = manager.PermissionState(options)
                    status := $"Permission state: {string state}."
            with e ->
                status := $"Permission query failed: {e.Message}"
        }

    let readSubscription () =
        promise {
            try
                if isNull registration then
                    subscription := "No service worker registration yet."
                else
                    let manager = pushManager registration
                    let! existing = manager.GetSubscription()
                    if isNull (box existing) || Optional.isUndefined existing then
                        subscription := "No subscription yet."
                    else
                        let sub = existing.Value
                        if isNull sub then subscription := "No subscription yet."
                        else subscription := $"Endpoint: {sub.Endpoint}"
            with e ->
                subscription := $"Could not read the subscription: {e.Message}"
        }

    let subscribe () =
        promise {
            try
                if isNull registration then
                    status := "No service worker registration yet."
                else
                    let manager = pushManager registration
                    let options = new PushSubscriptionOptions(UserVisibleOnly = true)
                    let! created = manager.Subscribe(options)
                    subscription := $"Endpoint: {created.Endpoint}"
                    status := "Subscription created."
            with e ->
                subscription := "Subscribe failed."
                status := $"Subscribe failed: {e.Message} (a push service is required)."
        }

    let readSyntheticEvent () =
        if pushEventExposed () <> "function" then
            payload := "This page does not expose the PushEvent constructor; it is only available inside a service worker."
        else
            try
                let event =
                    new PushEvent("push", new PushEventInit(Data = "hello from a synthetic PushEvent"))
                let data = event.Data
                if Optional.isUndefined data then payload := "No payload."
                else payload := data.Value.Text()
            with e ->
                payload := $"Could not build the PushEvent: {e.Message}"

    [<SPAEntryPoint>]
    let Main () =
        encodings := describeEncodings ()
        readSyntheticEvent ()

        registerWorker () |> ignore

        IndexTemplate()
            .encodings(encodings.V)
            .status(status.V)
            .subscription(subscription.V)
            .payload(payload.V)
            .checkPermission(fun _ -> checkPermission () |> ignore)
            .readSubscription(fun _ -> readSubscription () |> ignore)
            .subscribe(fun _ -> subscribe () |> ignore)
            .syntheticEvent(fun _ -> readSyntheticEvent ())
            .Bind()
