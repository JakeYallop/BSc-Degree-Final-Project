import { Http } from "./Http/Http.ts";
import redirectionMiddleware from "./Http/redirectionMiddleware.ts";
import retryMiddleware from "./Http/retryMiddleware.ts";

const getHttpClient = () => {
	return Http.getExecutor().with(retryMiddleware).with(redirectionMiddleware);
};

export default getHttpClient;
