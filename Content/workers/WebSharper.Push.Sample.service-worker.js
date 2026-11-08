import Runtime from "../WebSharper.Core.JavaScript/Runtime.js"
Runtime.ScriptBasePath="";
Runtime.ScriptSkipAssemblyDir=true;
function EntryPoint(self_1){
  self_1.oninstall=() => {
    self_1.skipWaiting();
  };
  self_1.onactivate=() => {
    self_1.clients.claim();
  };
  self_1.onpush=(event) => {
    let r;
    const body=payloadText(event);
    console.log("Push event received: "+body);
    const options=(r={},r.body=body,r.tag="websharper-push-demo",r);
    return event.waitUntil(self_1.registration.showNotification("Push received", options));
  };
  self_1.onnotificationclick=(event) => {
    event.notification.close();
    self_1.clients.openWindow("../../");
  };
}
function payloadText(event){
  const data=event.data;
  return data!==void 0?data.text():"No payload";
}
EntryPoint(globalThis);

