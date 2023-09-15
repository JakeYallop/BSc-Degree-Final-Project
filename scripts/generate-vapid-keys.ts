import webpush from "web-push";
import { writeConfig } from "./writeConfig.js";
import { resolveEnv } from "./resolveEnv.js";

const { envFile: envPush, resolvedEnvPath: envPathPush } = resolveEnv("PushNotifications");
const { envFile: envClient, resolvedEnvPath: envPathClient } = resolveEnv("Client");

const VAPID_PUBLIC_KEY = "VITE_VAPID_PUBLIC_KEY";
const VAPID_PRIVATE_KEY = "VITE_VAPID_PRIVATE_KEY";

console.log("Setting up VAPID keys...");
console.log("Generating VAPID keys...");
const keys = webpush.generateVAPIDKeys();

if (!envClient[VAPID_PUBLIC_KEY]) {
	console.log("Writing public key to client...");
	envClient[VAPID_PUBLIC_KEY] = keys.publicKey;
	writeConfig(envClient, envPathClient);
}

if (!envPush[VAPID_PUBLIC_KEY] || !envPush[VAPID_PRIVATE_KEY]) {
	console.log("Writing public and private keys to push notifications...");
	envPush[VAPID_PUBLIC_KEY] = keys.publicKey;
	envPush[VAPID_PRIVATE_KEY] = keys.privateKey;
	writeConfig(envPush, envPathPush);
}

console.log("VAPID keys successfully generated and written to .env and .env.local files.");
