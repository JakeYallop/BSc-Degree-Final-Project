/// <reference types="vite/client" />

interface ImportMetaEnv {
	readonly VITE_PORT: number;
	readonly VITE_VAPID_PUBLIC_KEY: string;
	readonly VITE_VAPID_PRIVATE_KEY: string;
	readonly VITE_CLIENT_URL: string;
}

interface ImportMeta {
	readonly env: ImportMetaEnv;
}
