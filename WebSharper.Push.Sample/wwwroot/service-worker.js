// Push event handler for the WebSharper Push API demo.
//
// The client registers this file, and the Push API delivers "push" events here
// when the browser's push service receives a message. A real push needs a
// VAPID key and a server that sends the encrypted payload, so this demo also
// builds a synthetic PushEvent in the page to exercise the binding.

self.addEventListener("install", () => {
  self.skipWaiting();
});

self.addEventListener("activate", (event) => {
  event.waitUntil(self.clients.claim());
});

self.addEventListener("push", (event) => {
  let body = "No payload";
  try {
    if (event.data) {
      body = event.data.text();
    }
  } catch (error) {
    body = "Unreadable payload: " + error;
  }

  console.log("Push event received:", body);

  event.waitUntil(
    self.registration.showNotification("Push received", {
      body: body,
      tag: "websharper-push-demo"
    })
  );
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();
  event.waitUntil(self.clients.openWindow("./"));
});
