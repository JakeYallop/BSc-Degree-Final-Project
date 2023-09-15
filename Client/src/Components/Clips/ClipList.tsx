import { Stack, StackProps } from "@mui/material";
import { ClipInfoItem } from "../../ClipsApi.ts";
import { useState } from "react";
import AsyncRender from "../AsyncRender.tsx";
import ClipItem from "./ClipItem.tsx";

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

export default ClipList;
