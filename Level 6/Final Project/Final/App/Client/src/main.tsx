import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App.tsx";
import { serviceWorkerFile } from "virtual:vite-plugin-service-worker";

import "@fontsource/roboto/300.css";
import "@fontsource/roboto/400.css";
import "@fontsource/roboto/500.css";
import "@fontsource/roboto/700.css";

ReactDOM.createRoot(document.getElementById("root")!).render(
	<React.StrictMode>
		<App />
	</React.StrictMode>
);

Notification.requestPermission()
	.then((permission) => {
		if (permission === "granted") {
			return navigator.serviceWorker.register(serviceWorkerFile, {
				type: "module",
				scope: "/",
			});
		} else {
			console.log("Notifications permission not granted");
		}
	})
	.catch((err) => {
		console.error("Service worker registration failed: ", err);
	});
