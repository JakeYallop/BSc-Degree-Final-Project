import TypedHeaders from "./TypedHeaders";

export class Http {
	private static defaultMiddleware: MiddlewareHandler[] = [];

	public static with(m: MiddlewareHandler) {
		this.defaultMiddleware.push(m);
	}

	public static getExecutor() {
		return new RequestExecutor([...this.defaultMiddleware]);
	}
}

export type NextDelegate = (context: ExecutionContext) => Promise<void>;
export type MiddlewareHandler = (context: ExecutionContext, next: NextDelegate) => Promise<void>;

type RequestInitWithCustomHeaders = Omit<RequestInit, "headers"> & {
	headers?: RequestInit["headers"] & { [key: string]: any };
};

type HttpMethods = "GET" | "POST" | "PATCH" | "PUT" | "DELETE" | "HEAD";
type RequestInitMethodRequired = Omit<RequestInitWithCustomHeaders, "method"> & {
	method: HttpMethods | string;
};

export class ExecutionContext {
	constructor(public url: string, options: RequestInitWithCustomHeaders) {
		this.request = {
			options,
			url,
		};
	}

	private _response: TypedResponse<unknown> | undefined;

	public data: any = {};
	public request: RequestContext;
	get response() {
		return this._response;
	}
	set response(value) {
		if (!this._response) {
			this._response = value;
		} else {
			throw new TypeError("response has already been set, and cannot be set again.");
		}
	}
	public error: unknown | undefined;
	public fetchErrored: boolean = false;
}

interface RequestContext {
	url: string;
	options: RequestInitWithCustomHeaders;
}

class RequestExecutor {
	constructor(private middleware: MiddlewareHandler[]) {}

	public with(m: MiddlewareHandler) {
		this.middleware.push(m);
		return this;
	}

	public get = <TResponse, TCustomOptions extends {} = {}>(
		url: string,
		options?: RequestInitWithCustomHeaders & TCustomOptions
	) => this.executor<TResponse>(url, options, "GET");

	public post = <TResponse, TCustomOptions extends {} = {}>(
		url: string,
		options?: RequestInitWithCustomHeaders & TCustomOptions
	) => this.executor<TResponse>(url, options, "POST");

	public delete = <TResponse>(url: string, options?: RequestInitWithCustomHeaders) =>
		this.executor<TResponse>(url, options, "DELETE");

	public postAsJson = <TResponse, TRequest = any>(
		url: string,
		data: TRequest,
		options?: RequestInitWithCustomHeaders
	) => this.jsonExecutor<TResponse>(url, data, options, "POST");

	public put = <TResponse, TCustomOptions extends {} = {}>(
		url: string,
		options?: RequestInitWithCustomHeaders & TCustomOptions
	) => this.executor<TResponse>(url, options, "PUT");

	public putAsJson = <TResponse, TRequest = any>(url: string, data: TRequest, options?: RequestInitWithCustomHeaders) =>
		this.jsonExecutor<TResponse>(url, data, options, "PUT");

	public patch = <T = any>(url: string, options?: RequestInitWithCustomHeaders) =>
		this.executor<T>(url, options, "PATCH");

	public patchAsJson = <TResponse, TRequest = any>(
		url: string,
		data: TRequest,
		options?: RequestInitWithCustomHeaders
	) => this.jsonExecutor<TResponse>(url, data, options, "PATCH");

	public request = <TResponse = any>(url: string, options: RequestInitMethodRequired) =>
		this.executor<TResponse>(url, options, options.method);

	private jsonExecutor = <TResponse, TRequest = any, TCustomOptions extends {} = {}>(
		url: string,
		data: TRequest,
		options: (RequestInitWithCustomHeaders & TCustomOptions) | undefined,
		method: string
	) => {
		return this.executor<TResponse>(
			url,
			{
				...options,
				body: JSON.stringify(data),
				headers: {
					...TypedHeaders.contentTypeJson,
				},
			},
			method
		);
	};

	private executor = async <T>(url: string, options: RequestInitWithCustomHeaders | undefined, method: string) => {
		const context = new ExecutionContext(url, { ...options, method });

		let pipeline: typeof fetchMiddleware = (context: ExecutionContext) => fetchMiddleware(context);
		for (let index = this.middleware.length - 1; index >= 0; index--) {
			const m = this.middleware[index];
			//prevent stale closure
			let delegate = pipeline;
			pipeline = (context: ExecutionContext) => m(context, delegate);
		}
		try {
			await pipeline(context);
		} catch (err: unknown) {
			if (!context.error) {
				context.error = err;
				context.fetchErrored = true;
			}
		}

		if (context.fetchErrored || context.error) {
			throw context.error;
		}
		return context.response as unknown as TypedResponse<T>;
	};
}

const fetchMiddleware = async <T>(context: ExecutionContext) => {
	try {
		const resp = await fetch(context.request.url, context.request.options);
		context.response = resp as TypedResponse<T>;
	} catch (err: unknown) {
		context.error = err;
		context.fetchErrored = true;
		console.error(err);
	}
};

type TypedResponse<T> = Omit<Response, "json"> & { json: () => Promise<T> };
