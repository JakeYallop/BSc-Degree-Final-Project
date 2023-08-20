import { defineConfig } from "vite";

// https://vitejs.dev/config/
export default defineConfig({
	plugins: [],
	build: {
		sourcemap: true,
	},
	optimizeDeps: {
		disabled: true,
	},
});
