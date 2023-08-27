import webpush from "web-push";
import fs from "fs";
import path from "path";
import dotenv from "dotenv";

function resolveEnv(packageName: string) {
	const envPath = `./${packageName}/.env`;
	const envLocalPath = `./${packageName}/.env.local`;

	let envFile: string;
	let resolvedEnvPath: string;
	try {
		if (fs.existsSync(envLocalPath)) {
			envFile = fs.readFileSync(envLocalPath, "utf-8");
			resolvedEnvPath = path.resolve(envLocalPath);
		} else if (fs.existsSync(envPath)) {
			envFile = fs.readFileSync(envPath, "utf-8");
			resolvedEnvPath = path.resolve(envPath);
		} else {
			throw "Could not find .env or .env.local file.";
		}
	} catch (err) {
		console.error(
			`Error reading .env or .env.local file in package ${packageName}. Make sure at least one of these files exists in both client and push notification projects`
		);

		console.error(err);
		process.exit(1);
	}

	return { envFile: dotenv.parse(envFile), resolvedEnvPath };
}

const { envFile: envPush, resolvedEnvPath: envPathPush } = resolveEnv("PushNotifications");
const { envFile: envClient, resolvedEnvPath: envPathClient } = resolveEnv("Client");

const VAPID_PUBLIC_KEY = "VITE_VAPID_PUBLIC_KEY";
const VAPID_PRIVATE_KEY = "VITE_VAPID_PRIVATE_KEY";

if (false && envPush[VAPID_PUBLIC_KEY] && envPush[VAPID_PRIVATE_KEY] && envClient[VAPID_PUBLIC_KEY]) {
	console.log("VAPID keys already exist in .env or .env.local files.");
	process.exit(0);
}

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

function writeConfig(config: Record<string, any>, path: string) {
	fs.writeFileSync(
		path,
		Object.entries(config)
			.map(([key, value]) => `${key}=${value}`)
			.join("\n")
	);
}

console.log("VAPID keys successfully generated and written to .env and .env.local files.");
