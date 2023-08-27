import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";
import { serviceWorkerPlugin as serviceWorker } from "@gautemo/vite-plugin-service-worker";

// https://vitejs.dev/config/
export default defineConfig({
	plugins: [
		react(),
		serviceWorker({
			filename: "sw.ts",
		}),
	],
	build: {
		sourcemap: true,
	},
});
