declare var self: ServiceWorkerGlobalScope;
export {};

self.addEventListener("push", (event: PushEvent) => {
	const data = event.data?.json();
	console.log("New notification", data);

	//https://developer.mozilla.org/en-US/docs/Web/API/ServiceWorkerRegistration/showNotification
	const options = {
		body: data.body,
		...(data.image && { image: data.image }),
		requireInteraction: true,
		renotify: true,
		tag: "motion-detected",
		vibrate: [400, 100, 500],
	};

	self.registration.showNotification(data.title, options);
});

self.registration.pushManager.getSubscription().then((subscription) => {});
