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
// (WebSharper.Push.Sample.service-worker.js) and the browser runs it. The
// script is exactly what a service worker needs, so we do not keep the
// dedicated worker: we hand the generated script to navigator.serviceWorker.
//
// The service-worker APIs (ServiceWorkerGlobalScope, Clients, the lifecycle
// events) come from the WebSharper.ServiceWorker binding; the push-specific
// accessors (pushManager, the push listener, waitUntil, closeNotification) come
// from WebSharper.Push.Extensions.
// ---------------------------------------------------------------------------
[<JavaScript>]
module WorkerEntry =
    open WebSharper.ServiceWorker
    open WebSharper.Push.Extensions

    /// Reads the text payload of a push event through the binding's PushMessageData.
    let payloadText (event: PushEvent) =
        let data = event.Data
        if Optional.isDefined data then data.Value.Text() else "No payload"

    let EntryPoint (self: DedicatedWorkerGlobalScope) =
        let scope = As<ServiceWorkerGlobalScope> (box self)
        scope.OnInstall <- fun _ -> scope.SkipWaiting() |> ignore
        scope.OnActivate <- fun _ -> scope.Clients.Claim() |> ignore
        scope.OnPush <- fun event ->
            let body = payloadText (As<PushEvent> (box event))
            Console.Log("Push event received: " + body)
            let options = new NotificationOptions(Body = body, Tag = "websharper-push-demo")
            event.WaitUntil(scope.Registration.ShowNotification("Push received", options))
        scope.OnNotificationClick <- fun event ->
            closeNotification event
            scope.Clients.OpenWindow("../../") |> ignore

[<JavaScript>]
module Client =
    open WebSharper.ServiceWorker
    open WebSharper.Push.Extensions

    // The template is loaded from the DOM, so editing index.html and refreshing
    // the browser is enough; a rebuild is only needed when holes change.
    type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument>

    let status = Var.Create "Not started."
    let encodings = Var.Create "—"
    let subscription = Var.Create "No subscription yet."
    let payload = Var.Create "No payload yet."

    let mutable registration : ServiceWorkerRegistration = null

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
                let worker = new Worker("service-worker", fun self -> WorkerEntry.EntryPoint self)
                worker.Terminate()
                let! reg =
                    JS.Window.Navigator.ServiceWorker.Register(
                        "Content/workers/WebSharper.Push.Sample.service-worker.js",
                        new ServiceWorkerRegistrationOptions(Type = WorkerType.Module))
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
        let constructor : obj = JS.Global?PushEvent
        if constructor ===. JS.Undefined then
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
