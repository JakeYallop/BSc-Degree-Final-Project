import { Box } from "@mui/material";
import { MediaPlayer, MediaOutlet } from "@vidstack/react";
import { ClipData } from "../../ClipsApi.ts";

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

export default VideoPlayer;
