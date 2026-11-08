import Runtime from "../WebSharper.Core.JavaScript/Runtime.js"
Runtime.ScriptBasePath="";
Runtime.ScriptSkipAssemblyDir=true;
function EntryPoint(self_1){
  self_1.addEventListener("install", () => {
    self_1.skipWaiting();
  });
  self_1.addEventListener("activate", () => {
    self_1.clients.claim();
  });
  self_1.addEventListener("push", (event) => {
    const body=payloadText(event);
    console.log("Push event received: "+body);
    event.waitUntil(self_1.registration.showNotification("Push received", {body:body, tag:"websharper-push-demo"}));
  });
  self_1.addEventListener("notificationclick", (event) => {
    event.notification.close();
    event.waitUntil(self_1.clients.openWindow("../../"));
  });
}
function payloadText(event){
  const data=event.data;
  return data!==void 0?data.text():"No payload";
}
EntryPoint(globalThis);

