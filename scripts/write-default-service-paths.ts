import { resolveEnv } from "./resolveEnv.js";
import packageJson from "../package.json";
import { writeConfig } from "./writeConfig.js";

const { envFile: envPush, resolvedEnvPath: envPathPush } = resolveEnv("PushNotifications");
const { envFile: envClient, resolvedEnvPath: envPathClient } = resolveEnv("Client");

console.log("Setting up default host paths...");

const VITE_API_BASE_URL = "VITE_API_BASE_URL";
const VITE_PUSH_SERVER_BASE_URL = "VITE_PUSH_SERVER_BASE_URL";
const VITE_CLIENT_BASE_URL = "VITE_CLIENT_BASE_URL";

if (!envClient[VITE_API_BASE_URL]) {
	console.log("Writing API base URL to client...");
	envClient[VITE_API_BASE_URL] = packageJson.defaultUrls.api;
	writeConfig(envClient, envPathClient);
}

if (!envClient[VITE_PUSH_SERVER_BASE_URL]) {
	console.log("Writing push server base URL to client...");
	envClient[VITE_PUSH_SERVER_BASE_URL] = packageJson.defaultUrls.pushNotifications;
	writeConfig(envClient, envPathClient);
}

if (!envPush[VITE_CLIENT_BASE_URL]) {
	console.log("Writing client base URL to push notifications...");
	envPush[VITE_CLIENT_BASE_URL] = packageJson.defaultUrls.client;
	writeConfig(envPush, envPathPush);
}

console.log("Completed writing url environment variables to .env files.");
