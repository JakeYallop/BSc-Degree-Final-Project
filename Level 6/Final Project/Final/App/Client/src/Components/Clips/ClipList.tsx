import { Box, Divider, Paper, Stack, StackProps, Typography, darken, lighten, styled, useTheme } from "@mui/material";
import ClipsApi, { ClipInfoItem } from "../../ClipsApi.ts";
import { useEffect, useState } from "react";
import AsyncRender from "../AsyncRender.tsx";
import LoadableImage from "../LoadableImage.tsx";
import Thumbnail from "../Thumbnail.tsx";
import { formatDate } from "../FormattedDate.ts";

export interface ClipListProps extends Omit<StackProps, "children" | "direction"> {
	clips: ClipInfoItem[] | null;
	onClipSelected?: (id: string) => void;
}
const ClipList = (props: ClipListProps) => {
	const [activeId, setActiveId] = useState<string | null>(null);
	const { clips, onClipSelected, ...rest } = props;

	const handleClipClick = (id: string) => {
		setActiveId(id);
		onClipSelected?.(id);
	};

	return (
		<Stack spacing={0} {...rest}>
			<AsyncRender loading={!clips}>
				{clips &&
					clips.map((c) => {
						return <ClipItem key={c.id} clip={c} active={activeId == c.id} onClick={handleClipClick} />;
					})}
			</AsyncRender>
		</Stack>
	);
};

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

interface ClipItemProps {
	clip: ClipInfoItem;
	active?: boolean;
	onClick?: (id: string) => void;
}
const ClipItem = (props: ClipItemProps) => {
	const { clip, active, onClick } = props;
	const theme = useTheme();

	return (
		<ClipItemContainer elevation={0} active={active} onClick={() => onClick && onClick(clip.id)}>
			<Stack spacing={1} direction="row">
				<Thumbnail displayEmpty={true} src={clip.thumbnail} />
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

export default ClipList;
