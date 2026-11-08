namespace WebSharper.Push.Sample

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Notation
open WebSharper.UI.Client
open WebSharper.UI.Templating
open WebSharper.Push

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

    [<Inline "$sw.register($path)">]
    let register (sw: obj) (path: string) : Promise<obj> = X<Promise<obj>>

    [<Inline "$sw.ready">]
    let ready (sw: obj) : Promise<obj> = X<Promise<obj>>

    [<Inline "$reg.pushManager">]
    let pushManager (reg: obj) : PushManager = X<PushManager>

    [<Inline "typeof globalThis.PushEvent">]
    let pushEventExposed () : string = X<string>

    let describeEncodings () =
        let encodings = PushManager.SupportedContentEncodings
        if isNull encodings || encodings.Length = 0 then "None reported."
        else String.concat ", " encodings

    let registerWorker () =
        promise {
            try
                let sw = serviceWorker JS.Window.Navigator
                let! _ = register sw "service-worker.js"
                status := "Service worker registered."
            with e ->
                status := $"Service worker registration failed: {e.Message}"
        }

    let checkPermission () =
        promise {
            try
                let sw = serviceWorker JS.Window.Navigator
                let! reg = ready sw
                let manager = pushManager reg
                let options = new PushSubscriptionOptions(UserVisibleOnly = true)
                let! state = manager.PermissionState(options)
                status := $"Permission state: {string state}."
            with e ->
                status := $"Permission query failed: {e.Message}"
        }

    let readSubscription () =
        promise {
            try
                let sw = serviceWorker JS.Window.Navigator
                let! reg = ready sw
                let manager = pushManager reg
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
                let sw = serviceWorker JS.Window.Navigator
                let! reg = ready sw
                let manager = pushManager reg
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
