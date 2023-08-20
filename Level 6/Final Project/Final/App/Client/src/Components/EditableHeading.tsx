import { Save, Cancel, Edit } from "@mui/icons-material";
import { Stack, TextField, Tooltip, IconButton, Typography } from "@mui/material";
import { Variant } from "@mui/material/styles/createTypography";
import { useState, useRef } from "react";

interface EditableHeadingProps {
	variant?: Variant;
	prefix?: string;
	heading: string;
	onSave: (s: string) => void;
}
const EditableHeading = (props: EditableHeadingProps) => {
	const { variant = "h4", heading, prefix, onSave } = props;
	const [editMode, setEditMode] = useState(false);
	const [pendingEdit, setPendingEdit] = useState(heading);
	const titleRef = useRef<HTMLDivElement>(null);

	const toggleEditField = () => {
		setEditMode(!editMode);
		setPendingEdit(heading);
	};

	const onTextChange = (e: React.ChangeEvent<HTMLInputElement>) => {
		setPendingEdit(e.target.value || "");
	};

	const onSaveClick = () => {
		onSave(pendingEdit);
		toggleEditField();
	};

	const onCancelClick = () => {
		toggleEditField();
	};

	return (
		<Stack direction="row" alignItems="center">
			{prefix && (
				<Typography variant={variant} display="inline">
					{prefix}:&nbsp;
				</Typography>
			)}
			{editMode ? (
				<>
					<TextField value={pendingEdit} onChange={onTextChange} sx={{ minWidth: "20rem" }} />
					<Tooltip title="Save">
						<IconButton onClick={onSaveClick}>
							<Save />
						</IconButton>
					</Tooltip>
					<Tooltip title="Cancel">
						<IconButton onClick={onCancelClick}>
							<Cancel />
						</IconButton>
					</Tooltip>
				</>
			) : (
				<>
					<Typography variant={variant} ref={titleRef} display="inline-block" sx={{ minWidth: "20rem" }}>
						{heading}
					</Typography>
					<Tooltip title="Edit">
						<IconButton onClick={toggleEditField}>
							<Edit />
						</IconButton>
					</Tooltip>
				</>
			)}
		</Stack>
	);
};

export default EditableHeading;
