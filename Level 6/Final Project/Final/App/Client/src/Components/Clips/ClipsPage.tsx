import { useEffect, useState } from "react";
import ClipsApi, { ClipInfoItem } from "../../ClipsApi.ts";
import ClipList from "./ClipList.tsx";

const ClipsPage = () => {
	const [clips, setClips] = useState<ClipInfoItem[] | null>(null);
	const [clipsSentinel, setClipsSentinel] = useState(true);

	useEffect(() => {
		ClipsApi.getClips().then((clips) => {
			setClips(clips);
		});
	}, [clipsSentinel]);

	return <ClipList clips={clips}></ClipList>;
};

export default ClipsPage;
