import getHttpClient from "./getHttpClient.ts";

const ApiBaseUrl = `${import.meta.env.VITE_API_BASE_URL}/clips`;

const u = (endpoint?: string) => `${ApiBaseUrl}${endpoint ? `/${endpoint}` : ""}`;

const http = getHttpClient();

export interface ClipInfoItem {
	id: string;
	dateRecorded: Date;
	name: string;
	thumbnail: string | undefined;
}

const getClips = async () => {
	const data = await http.get<ClipInfoItem[]>(u());
	return data.json();
};

const ClipsApi = {
	getClips,
};

export default ClipsApi;
