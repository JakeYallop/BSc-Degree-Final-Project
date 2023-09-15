import fs from "fs";
import path from "path";
import dotenv from "dotenv";

export function resolveEnv(packageName: string) {
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
			fs.writeFileSync(envLocalPath, "");
			envFile = "";
			resolvedEnvPath = path.resolve(envLocalPath);
		}
	} catch (err) {
		console.error(`Unexpected error generating keys for package ${packageName}.`);

		console.error(err);
		process.exit(1);
	}

	return { envFile: dotenv.parse(envFile), resolvedEnvPath };
}
