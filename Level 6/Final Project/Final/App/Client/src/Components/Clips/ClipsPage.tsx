import { useCallback, useEffect, useState } from "react";
import ClipsApi, { ClipData, ClipInfoItem } from "../../ClipsApi.ts";
import ClipList from "./ClipList.tsx";
import { Box, Stack, StackProps, Typography } from "@mui/material";
import { MediaPlayer, MediaOutlet } from "@vidstack/react";
import "vidstack/styles/defaults.css";
import EditableHeading from "../EditableHeading.tsx";
import { formatDate } from "../FormattedDate.ts";
import * as signalR from "@microsoft/signalr";

const fetchClip = async (selectedClipId: string) => {
	const response = await ClipsApi.getClip(selectedClipId);
	if (response.ok) {
		return await response.json();
	} else {
		const text = await response.text();
		console.error("Error loading clip \r\n" + text);
	}
	return null;
};

const ClipsPage = () => {
	const [clips, setClips] = useState<ClipInfoItem[] | null>(null);
	const [selectedClipId, setSelectedClipId] = useState<string | null>(null);
	const [clip, setClip] = useState<ClipData | null>(null);
	const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

	useEffect(() => {
		ClipsApi.getClips().then((clips) => {
			setClips(clips);
		});
	}, []);

	useEffect(() => {
		const getClip = async () => {
			if (selectedClipId) {
				const clip = await fetchClip(selectedClipId);
				setClip(clip);
			}
		};
		getClip();
	}, [selectedClipId]);

	const handleClipSelected = (id: string) => {
		setSelectedClipId(id);
	};

	const handleClipUpdated = useCallback(
		async (clipId: string) => {
			if (clipId == selectedClipId) {
				const clip = await fetchClip(clipId);
				var oldClip = clips!.find((c) => c.id == clipId)!;
				setClip(clip);
				setClips(
					[
						...clips!.filter((c) => c.id != clipId),
						{ id: clip!.id, dateRecorded: oldClip.dateRecorded, name: clip?.name!, thumbnail: oldClip.thumbnail! },
					].sort((a, b) => (a.dateRecorded > b.dateRecorded ? 1 : -1))
				);
			}
		},
		[selectedClipId, clip, clips]
	);

	useEffect(() => {
		const connection = new signalR.HubConnectionBuilder()
			.withUrl(`${import.meta.env.VITE_API_BASE_URL}/clipHub`, {
				transport: signalR.HttpTransportType.WebSockets,
				skipNegotiation: true,
				withCredentials: true,
			})
			.withAutomaticReconnect()
			.configureLogging(signalR.LogLevel.Information)
			.build();

		connection.on("NewClipAdded", (_: string) => {
			ClipsApi.getClips().then((clips) => {
				setClips(clips);
			});
		});
		connection.start();
		setConnection(connection);
	}, []);

	useEffect(() => {
		connection?.off("ClipUpdated");
		connection?.on("ClipUpdated", handleClipUpdated);
		if (connection?.state == signalR.HubConnectionState.Disconnected) {
			connection?.start();
		}
	}, [handleClipUpdated]);

	return (
		<Stack direction="row" spacing={1}>
			<ClipList minWidth="300px" clips={clips} onClipSelected={handleClipSelected}></ClipList>
			{clip && (
				<Box width="100%">
					<ClipView clip={clip!}></ClipView>
				</Box>
			)}
		</Stack>
	);
};

export default ClipsPage;

interface ClipViewProps extends StackProps {
	clip: ClipData;
}

const ClipView = (props: ClipViewProps) => {
	const { clip, ...rest } = props;
	const handleSave = async (s: string) => {
		await ClipsApi.updateName(clip.id, s);
	};

	return (
		<Stack {...rest} spacing={2}>
			<VideoPlayer clip={clip} />
			<EditableHeading heading={clip.name} prefix="Clip Name" onSave={handleSave} />
			<Typography variant="h6">Date Recorded: {formatDate(clip.dateRecorded)}</Typography>
		</Stack>
	);
};

interface VideoPlayerProps {
	clip: ClipData;
}
const VideoPlayer = (props: VideoPlayerProps) => {
	const {
		clip: { url },
	} = props;

	return (
		<Box maxWidth="80%" maxHeight="50%">
			<MediaPlayer title="Video" src={url} controls>
				<MediaOutlet />
			</MediaPlayer>
		</Box>
	);
};
