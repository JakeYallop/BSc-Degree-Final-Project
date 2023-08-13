import { useEffect, useState } from "react";
import ClipsApi, { ClipData, ClipInfoItem } from "../../ClipsApi.ts";
import ClipList from "./ClipList.tsx";

const ClipsPage = () => {
	const [clips, setClips] = useState<ClipInfoItem[] | null>(null);
	const [selectedClipId, setSelectedClipId] = useState<string | null>(null);
	const [clip, setClip] = useState<ClipData | null>(null);
	const [clipsSentinel, setClipsSentinel] = useState(true);

	useEffect(() => {
		ClipsApi.getClips().then((clips) => {
			setClips(clips);
		});
	}, [clipsSentinel]);

	useEffect(() => {
		const getClip = async () => {
			if (selectedClipId) {
				var response = await ClipsApi.getClip(selectedClipId);
				if (response.ok) {
					var clip = await response.json();
					setClip(clip);
				} else {
					var text = await response.text();
					console.error("Error loading clip \r\n" + text);
				}
			}
		};
		getClip();
	}, [selectedClipId]);

	return <ClipList clips={clips}></ClipList>;
};

export default ClipsPage;

interface ClipViewProps {
	clip: ClipData;
}

const ClipView = (props: ClipViewProps) => {
	return <VideoPlayer clip={props.clip} />;
};

interface VideoPlayerProps {
	clip: ClipData;
}
const VideoPlayer = (props: VideoPlayerProps) => {
	return <></>;
};
