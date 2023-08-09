import { Menu } from "@mui/icons-material";
import { useTheme, Box, IconButton } from "@mui/material";

interface SidebarSlimProps {
	open?: boolean;
	handleOpen: () => void;
	width?: number;
}
const SidebarSlim = (props: SidebarSlimProps) => {
	const theme = useTheme();
	const { open, handleOpen } = props;
	return (
		<Box
			sx={{
				width: "40px",
				height: "100%",
				display: open ? "block" : "none",
				position: "fixed",
				top: "0",
				left: "0",
				borderRight: `1px solid ${theme.palette.divider}`,
			}}
		>
			<IconButton onClick={handleOpen}>
				<Menu />
			</IconButton>
		</Box>
	);
};

export default SidebarSlim;
