import { StackProps, Stack, Typography } from "@mui/material";
import ClipsApi, { ClipData } from "../../ClipsApi.ts";
import EditableHeading from "../EditableHeading.tsx";
import { formatDate } from "../FormattedDate.ts";
import VideoPlayer from "./VidepPlayer.tsx";

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

export default ClipView;
