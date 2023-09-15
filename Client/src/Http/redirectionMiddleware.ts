import { MiddlewareHandler } from "./Http";

const redirectionMiddleware: MiddlewareHandler = async (context, next) => {
    //execute the fetch request
    await next(context);
    //deal with the response
    if (!context.fetchErrored) {
        const status = context.response?.status;
        if (status === 301 || status === 302 || context.response?.redirected) {
            const url = context.response!.url;
            if (process.env.NODE_ENV !== "production") {
                if (!url) {
                    throw new Error("redirectionMiddleware: Unexpected undefined url.");
                }
            }
            window.location.href = url;
        }
    }
};

export default redirectionMiddleware;
