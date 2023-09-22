import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App.tsx";
import { serviceWorkerFile } from "virtual:vite-plugin-service-worker";

import "@fontsource/roboto/300.css";
import "@fontsource/roboto/400.css";
import "@fontsource/roboto/500.css";
import "@fontsource/roboto/700.css";
import NotificationsApi from "./NotificationsApi.ts";
import base64UrlToBytes from "./base64ToBytes.ts";

ReactDOM.createRoot(document.getElementById("root")!).render(
	<React.StrictMode>
		<App />
	</React.StrictMode>
);

let sw: { registration: ServiceWorkerRegistration | null } = {
	registration: null,
};

export const registerForPushNotifications = () => {
	Notification.requestPermission()
		.then((permission) => {
			console.log("Permissions: ", permission);
			if (permission === "granted") {
				return navigator.serviceWorker.register(serviceWorkerFile, {
					type: "module",
					scope: "/",
				});
			} else {
				console.log("Notifications permission not granted");
				return Promise.reject();
			}
		})
		.then((registration) => {
			sw.registration = registration;

			return sw.registration.pushManager.subscribe({
				userVisibleOnly: true,
				applicationServerKey: base64UrlToBytes(import.meta.env.VITE_VAPID_PUBLIC_KEY),
			});
		})
		.then((subscription) => {
			return NotificationsApi.subscribe(subscription);
		})
		.then(() => {
			console.log("Subscribed to push notifications");
		})
		.catch((err) => {
			console.error("Service worker registration failed: ", err);
		});
};
