import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";
import mkcert from "vite-plugin-mkcert";
import { serviceWorkerPlugin as serviceWorker } from "@gautemo/vite-plugin-service-worker";

// https://vitejs.dev/config/
export default defineConfig({
	plugins: [
		react(),
		mkcert(),
		serviceWorker({
			filename: "sw.ts",
		}),
	],
	build: {
		sourcemap: true,
	},
	server: {
		https: true,
	},
});
