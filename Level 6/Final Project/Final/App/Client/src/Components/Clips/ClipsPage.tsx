import { useCallback, useEffect, useRef, useState } from "react";
import ClipsApi, { ClipData, ClipInfoItem } from "../../ClipsApi.ts";
import ClipList from "./ClipList.tsx";
import { Box, Stack, StackProps, Typography } from "@mui/material";
import { MediaPlayer, MediaOutlet } from "@vidstack/react";
import "vidstack/styles/defaults.css";
import EditableHeading from "../EditableHeading.tsx";
import { formatDate } from "../FormattedDate.ts";
import * as signalR from "@microsoft/signalr";
import ClipView from "./ClipView.tsx";
import usePrevious from "../../Hooks/usePrevious.ts";
import useEventCallback from "../../Hooks/useEventCallback.ts";

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

	const handleClipUpdated = useEventCallback(async (clipId: string) => {
		if (clipId == selectedClipId) {
			const clip = await fetchClip(clipId);
			const oldClip = clips!.find((c) => c.id == clipId)!;
			setClip(clip);
			setClips(
				[
					...clips!.filter((c) => c.id != clipId),
					{ id: clip!.id, dateRecorded: oldClip.dateRecorded, name: clip?.name!, thumbnail: oldClip.thumbnail! },
				].sort((a, b) => (a.dateRecorded > b.dateRecorded ? 1 : -1))
			);
		}
	});

	const handleClipAdded = useEventCallback((clipId: string) => {
		ClipsApi.getClip(clipId)
			.then((clip) => {
				return clip.json();
			})
			.then((clip) => {
				const newClips = [
					...clips!.filter((c) => c.id != clip.id),
					{
						id: clip.id,
						dateRecorded: new Date(clip.dateRecorded),
						name: clip.name,
						thumbnail: clip.thumbnail,
					},
				].sort((a, b) => (a.dateRecorded > b.dateRecorded ? 1 : -1));
				setClips(newClips);
			});
	});

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

		connection.start();
		setConnection(connection);
	}, []);

	useEffect(() => {
		if (connection != null) {
			connection?.on("NewClipAdded", handleClipAdded);
			connection?.on("AdditionalTasksCompleted", handleClipAdded);
			if (connection?.state == signalR.HubConnectionState.Disconnected) {
				connection?.start();
			}
		}
	}, [connection]);

	useEffect(() => {
		if (connection != null) {
			connection?.on("ClipUpdated", handleClipUpdated);
			if (connection?.state == signalR.HubConnectionState.Disconnected) {
				connection?.start();
			}
		}
	}, [connection]);

	return (
		<Stack direction="row" spacing={1}>
			<ClipList minWidth="300px" clips={clips} onClipSelected={handleClipSelected}></ClipList>
			{clip && (
				<Box width="100%" position="relative">
					<Box width="100%" position="sticky" top="0">
						<ClipView clip={clip!}></ClipView>
					</Box>
				</Box>
			)}
		</Stack>
	);
};

export default ClipsPage;
