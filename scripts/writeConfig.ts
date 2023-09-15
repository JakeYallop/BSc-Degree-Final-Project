import fs from "fs";

export function writeConfig(config: Record<string, any>, path: string) {
	fs.writeFileSync(
		path,
		Object.entries(config)
			.map(([key, value]) => `${key}=${value}`)
			.join("\n")
	);
}
