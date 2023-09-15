import { ExecutionContext, MiddlewareHandler, NextDelegate } from "./Http";

const retries = [500, 1000, 4000];
const randomJitter = 100;
const retryMiddleware: MiddlewareHandler = async (context, next) => {
	//prevent infinite loops
	if (context.data.startedRetry) {
		await execute(context, next);
		return;
	}

	for (const retryDelay of retries) {
		if (!context.data.startedRetry) {
			context.data.startedRetry = true;
		}
		const success = await execute(context, next);
		if (success) {
			context.fetchErrored = false;
			context.error = undefined;
			return;
		}
		const resultingDelay = retryDelay + Math.ceil(Math.random() * randomJitter);
		await wait(resultingDelay);
	}
	await execute(context, next);
	return;
};

const wait = async (delay: number) => {
	return new Promise((resolve, _) => {
		setTimeout(resolve, delay);
	});
};

const execute = async (context: ExecutionContext, next: NextDelegate) => {
	try {
		await next(context);
		if (context.fetchErrored) {
			return false;
		}
		return true;
	} catch {
		return false;
	}
};

export default retryMiddleware;
