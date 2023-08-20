import Fastify from "fastify";

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

app.get("/", async (request, reply) => {
	return "Hello World";
});

app.post("/notifcation", async (request, reply) => {
	app.log.info("Received request to create notification");
});

try {
	await app.listen({
		port: import.meta.env.VITE_PORT || 3000,
	});
} catch (err) {
	app.log.error(err);
	process.exit(1);
}
