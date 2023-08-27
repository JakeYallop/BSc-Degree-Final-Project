import getHttpClient from "./getHttpClient.ts";

const ApiBaseUrl = `${import.meta.env.VITE_PUSH_SERVER_BASE_URL}`;
const u = (endpoint?: string) => `${ApiBaseUrl}${endpoint ? `/${endpoint}` : ""}`;
const http = getHttpClient();

const subscribe = (subscription: PushSubscription) => {
	return http.postAsJson(u("subscribe"), subscription);
};

const NotificationsApi = {
	subscribe,
};

export default NotificationsApi;
