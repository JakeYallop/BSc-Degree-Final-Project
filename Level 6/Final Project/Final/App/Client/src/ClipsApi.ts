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

export interface ClipData {
	id: string;
	name: string;
	detections: {
		timestamp: number;
		boundinBox: [x: number, y: number, w: number, h: number];
	};
	url: string;
}

const getClip = (id: string) => http.get<ClipData>(u(id));

const ClipsApi = {
	getClips,
	getClip,
};

export default ClipsApi;
