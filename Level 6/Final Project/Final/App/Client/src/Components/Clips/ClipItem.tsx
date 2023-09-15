import { styled, Paper, lighten, darken, Stack, Box, Typography, useTheme } from "@mui/material";
import { useEffect } from "react";
import { ClipInfoItem } from "../../ClipsApi.ts";
import { formatClassificationWithConfidence } from "../formatClassificationLabel.ts";
import { formatDate } from "../formatDate.ts";
import Thumbnail from "../Thumbnail.tsx";

interface ClipItemProps {
	clip: ClipInfoItem;
	active?: boolean;
	onClick?: (id: string) => void;
}

const ClipItemContainer = styled(Paper, { shouldForwardProp: (prop) => prop !== "active" })<{ active?: boolean }>(
	({ theme, active }) => ({
		cursor: "pointer",
		"&:hover": {
			backgroundColor:
				theme.palette.mode == "dark"
					? lighten(theme.palette.background.paper, 0.1)
					: darken(theme.palette.background.paper, 0.1),
		},
		padding: theme.spacing(1),
		...(active && {
			backgroundColor:
				theme.palette.mode == "dark"
					? lighten(theme.palette.background.paper, 0.15)
					: darken(theme.palette.background.paper, 0.15),
		}),
	})
);

const ClipItem = (props: ClipItemProps) => {
	const { clip, active, onClick } = props;
	const theme = useTheme();

	useEffect(() => {}, [clip]);

	return (
		<ClipItemContainer elevation={0} active={active} onClick={() => onClick && onClick(clip.id)}>
			<Stack spacing={1} direction="row">
				<Thumbnail displayEmpty={true} src={clip.thumbnail} thumbWidth={8} />
				<Stack spacing={1}>
					<Box>
						<Typography display="inline" variant="subtitle1">
							Clip Name:{" "}
						</Typography>
						<Typography display="inline" variant="body1">
							{clip.name}
						</Typography>
					</Box>
					<Box color={theme.palette.text.secondary}>
						<Typography display="inline" variant="subtitle2">
							Object detected:{" "}
						</Typography>
						<Typography display="inline" variant="body1">
							{formatClassificationWithConfidence(clip.classification)}
						</Typography>
					</Box>
					<Box color={theme.palette.text.secondary}>
						<Typography display="inline" variant="subtitle2">
							Date recorded:{" "}
						</Typography>
						<Typography display="inline" variant="body1">
							{formatDate(clip.dateRecorded)}
						</Typography>
					</Box>
				</Stack>
			</Stack>
		</ClipItemContainer>
	);
};

export default ClipItem;
