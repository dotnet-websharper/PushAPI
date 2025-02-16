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

    let ArrayOrString = T<string> + !|T<string>

    let PushSubscriptionOptions =
        Pattern.Config "PushSubscriptionOptions" {
            Required = []
            Optional = [
                "userVisibleOnly", T<bool>  
                "applicationServerKey", T<obj>  
            ]
        }

    let PushSubscription =
        Class "PushSubscription"
        |+> Instance [
            "endpoint" =? T<string>
            "expirationTime" =? T<float>  
            "options" =? PushSubscriptionOptions

            "getKey" => Enum.PushEncryptionKeys?name ^-> T<ArrayBuffer>  
            "toJSON" => T<unit> ^-> T<obj>  
            "unsubscribe" => T<unit> ^-> T<Promise<bool>>  
        ]

    let PushManager =
        Class "PushManager"
        |+> Static [
            "supportedContentEncodings" =? ArrayOrString
        ]
        |+> Instance [
            "getSubscription" => T<unit> ^-> T<Promise<_>>[PushSubscription]              
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

    let PushEvent =
        Class "PushEvent"
        |=> Inherits T<Dom.Event>  
        |+> Static [
            Constructor (T<string>?``type`` * !?T<obj>?options)
        ]
        |+> Instance [
            "data" =? PushMessageData.Type  
        ]


    let Assembly =
        Assembly [
            Namespace "WebSharper.Push" [
                PushEvent
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
