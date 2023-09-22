import Fastify from "fastify";
import cors from "@fastify/cors";
import webpush from "web-push";
import Database from "better-sqlite3";
import BetterSqlite3 from "better-sqlite3";

const loggerSettings = {
	development: {
		transport: {
			target: "pino-pretty",
			options: {
				translateTime: "HH:MM:ss Z",
				ignore: "pid,hostname",
			},
		},
	},
	production: true,
	test: false,
};

const db = new Database("pushnotifications.db", {
	fileMustExist: false,
});

ensureTablesExists(db);

function ensureTablesExists(db: BetterSqlite3.Database) {
	db.prepare("CREATE TABLE IF NOT EXISTS subscriptions (endpoint TEXT PRIMARY KEY, data TEXT)").run();
}

function insertSubscription(db: BetterSqlite3.Database, subscription: webpush.PushSubscription) {
	db.prepare("INSERT INTO subscriptions (endpoint, data) VALUES (?, ?)").run(
		subscription.endpoint,
		JSON.stringify(subscription)
	);
}

function removeSubscription(db: BetterSqlite3.Database, subscription: webpush.PushSubscription) {
	db.prepare("DELETE FROM subscriptions WHERE endpoint = ?").run(subscription.endpoint);
}

function fetchSubscriptions(db: BetterSqlite3.Database): webpush.PushSubscription[] {
	const data = db.prepare("SELECT * FROM subscriptions").all() as { endpoint: string; data: string }[];
	return data.map((row) => JSON.parse(row.data));
}

const app = Fastify({
	logger: loggerSettings[import.meta.env.MODE] || true,
});
console.log("Setting up CORS to allow access from origin: " + import.meta.env.VITE_CLIENT_BASE_URL);
await app.register(cors, {
	origin: import.meta.env.VITE_CLIENT_BASE_URL,
});

app.get("/", async (request, reply) => {
	return "Hello World";
});

interface NotificationBody {
	title?: string;
	body?: string;
}

app.post<{ Body: NotificationBody }>("/notify", async (request, reply) => {
	try {
		app.log.info("Received request to create notification");
		const subscriptions = fetchSubscriptions(db);
		for (let i = 0; i < subscriptions.length; i++) {
			const subscription = subscriptions[i];
			try {
				await webpush.sendNotification(
					subscription,
					JSON.stringify({
						title: request.body?.title || "",
						body: request.body?.body || "",
					}),
					{
						vapidDetails: {
							subject: "https://localhost:5173",
							publicKey: import.meta.env.VITE_VAPID_PUBLIC_KEY,
							privateKey: import.meta.env.VITE_VAPID_PRIVATE_KEY,
						},
					}
				);
			} catch (err) {
				if (err.statusCode && err.statusCode == 410) {
					removeSubscription(db, subscription);
					return;
				}
				throw err;
			}
		}
	} catch (err) {
		app.log.error(err);
	}
});

app.post("/subscribe", async (request, reply) => {
	app.log.info("Received request to create subscription");
	const subscription = request.body as webpush.PushSubscription;
	console.log(subscription);
	insertSubscription(db, subscription);
	return "Subscription created";
});

try {
	await app.listen({
		port: import.meta.env.VITE_PORT || 3000,
	});
} catch (err) {
	app.log.error(err);
	process.exit(1);
}
