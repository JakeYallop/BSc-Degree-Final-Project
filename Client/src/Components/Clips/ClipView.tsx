import { StackProps, Stack, Typography, Button, Accordion, AccordionSummary, AccordionDetails } from "@mui/material";
import ClipsApi, { ClipData } from "../../ClipsApi.ts";
import EditableHeading from "../EditableHeading.tsx";
import { formatDate } from "../formatDate.ts";
import VideoPlayer from "./VideoPlayer.tsx";
import { ClassificationLabel, formatClassificationWithConfidence } from "../formatClassificationLabel.ts";
import { useState } from "react";

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
			<ObjectDetectionInfo classifications={clip.classifications} />
		</Stack>
	);
};

interface ObjectDetectionInfoProps {
	classifications?: ClassificationLabel[];
}
const ObjectDetectionInfo = (props: ObjectDetectionInfoProps) => {
	const [expanded, setExpanded] = useState<boolean>(false);
	const { classifications } = props;

	if (!classifications) {
		return null;
	}

	const sortedClassifications = classifications.sort((a, b) => (a.confidence < b.confidence ? 1 : -1));

	return (
		<>
			<Typography variant="h6">
				Object detected: {formatClassificationWithConfidence(sortedClassifications[0], true)}
			</Typography>
			<Accordion expanded={expanded} onChange={(_, expanded) => setExpanded(expanded)}>
				<AccordionSummary>View all guesses</AccordionSummary>
				<AccordionDetails>
					<Stack spacing={2}>
						{sortedClassifications.map((c) => (
							<Typography>{formatClassificationWithConfidence(c, true)}</Typography>
						))}
					</Stack>
				</AccordionDetails>
			</Accordion>
		</>
	);
};

export default ClipView;
