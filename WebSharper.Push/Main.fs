namespace WebSharper.Push

open WebSharper
open WebSharper.JavaScript
open WebSharper.InterfaceGenerator

module Definition =

    module Enum =
        let PushPermissionState =
            Pattern.EnumStrings "PushPermissionState" [
                "granted"
                "denied"
                "prompt"
            ]

        let PushEncryptionKeys =
            Pattern.EnumStrings "PushEncryptionKeys" [
                "p256dh"
                "auth"
            ]

    let ArrayOfStrings = !|T<string>

    let PushSubscriptionOptions =
        Pattern.Config "PushSubscriptionOptions" {
            Required = []
            Optional = [
                "userVisibleOnly", T<bool>
                "applicationServerKey", T<ArrayBuffer> + T<ArrayBufferView> + T<string>
            ]
        }

    let PushSubscription =
        Class "PushSubscription"
        |+> Instance [
            "endpoint" =? T<string>
            "expirationTime" =? !?T<float>
            "options" =? PushSubscriptionOptions

            "getKey" => Enum.PushEncryptionKeys?name ^-> !?T<ArrayBuffer>
            "toJSON" => T<unit> ^-> T<obj>
            "unsubscribe" => T<unit> ^-> T<Promise<bool>>
        ]

    let PushManager =
        Class "PushManager"
        |+> Static [
            "supportedContentEncodings" =? ArrayOfStrings
        ]
        |+> Instance [
            "getSubscription" => T<unit> ^-> T<Promise<_>>[!?PushSubscription]
            "permissionState" => !?PushSubscriptionOptions?options ^-> T<Promise<_>>[Enum.PushPermissionState]
            "subscribe" => !?PushSubscriptionOptions?options ^-> T<Promise<_>>[PushSubscription]
        ]

    let PushMessageData =
        Class "PushMessageData"
        |+> Instance [
            "arrayBuffer" => T<unit> ^-> T<ArrayBuffer>
            "blob" => T<unit> ^-> T<Blob>
            "bytes" => T<unit> ^-> T<Uint8Array>
            "json" => T<unit> ^-> T<obj>
            "text" => T<unit> ^-> T<string>
        ]

    let PushEventInit =
        Pattern.Config "PushEventInit" {
            Required = []
            Optional = [
                "data", PushMessageData.Type + T<ArrayBuffer> + T<ArrayBufferView> + T<string>
                "bubbles", T<bool>
                "cancelable", T<bool>
                "composed", T<bool>
            ]
        }

    let PushEvent =
        Class "PushEvent"
        |=> Inherits T<Dom.Event>
        |+> Static [
            Constructor (T<string>?``type`` * !?PushEventInit?options)
        ]
        |+> Instance [
            "data" =? !?PushMessageData.Type
        ]


    let Assembly =
        Assembly [
            Namespace "WebSharper.Push" [
                PushEvent
                PushEventInit
                PushMessageData
                PushManager
                PushSubscription
                PushSubscriptionOptions
                Enum.PushEncryptionKeys
                Enum.PushPermissionState
            ]
        ]

[<Sealed>]
type Extension() =
    interface IExtension with
        member ext.Assembly =
            Definition.Assembly

[<assembly: Extension(typeof<Extension>)>]
do ()
