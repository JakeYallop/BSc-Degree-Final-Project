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
	const resp = await http.get<ClipInfoItem[]>(u());
	const data = await resp.json();
	return data.map((x) => {
		x.dateRecorded = new Date(x.dateRecorded);
		return x;
	});
};

export interface ClipData {
	id: string;
	name: string;
	dateRecorded: string;
	detections: {
		timestamp: number;
		boundinBox: [x: number, y: number, w: number, h: number];
	};
	url: string;
}

const getClip = (id: string) => http.get<ClipData>(u(id));

const updateName = (id: string, name: string) => http.putAsJson(u(`${id}/name`), { name });

const ClipsApi = {
	getClips,
	getClip,
	updateName,
};

export default ClipsApi;
