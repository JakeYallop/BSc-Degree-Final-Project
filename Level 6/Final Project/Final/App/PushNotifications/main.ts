import Fastify from "fastify";
import cors from "@fastify/cors";
import webpush from "web-push";

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

const app = Fastify({
	logger: loggerSettings[import.meta.env.MODE] || true,
});

await app.register(cors, {
	origin: import.meta.env.VITE_CLIENT_URL,
});

let subscription: webpush.PushSubscription;

app.get("/", async (request, reply) => {
	return "Hello World";
});

app.post("/notify", async (request, reply) => {
	try {
		app.log.info("Received request to create notification");
		webpush.sendNotification(
			subscription,
			JSON.stringify({
				title: "Test notiifcation",
				body: "This is a test notification",
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
		app.log.error(err);
	}
});

app.post("/subscribe", async (request, reply) => {
	app.log.info("Received request to create subscription");
	subscription = request.body as webpush.PushSubscription;
	console.log(subscription);
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
