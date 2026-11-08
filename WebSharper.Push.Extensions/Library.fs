namespace WebSharper.Push

open WebSharper
open WebSharper.JavaScript
open WebSharper.ServiceWorker

/// The bridge between the ServiceWorker binding's global scope and the Push
/// binding's payload types. The ServiceWorker API is not modelled by core and
/// the ServiceWorker binding has no Push types, so the push-specific accessors
/// live here as inlined functions.
[<JavaScript>]
module Extensions =

    /// The registration's PushManager. The ServiceWorker binding exposes it as obj.
    [<Inline "$registration.pushManager">]
    let pushManager (registration: ServiceWorkerRegistration) : PushManager = X<PushManager>

    /// Closes the notification of a notificationclick event.
    [<Inline "$event.notification.close()">]
    let closeNotification (event: obj) : unit = X<unit>
