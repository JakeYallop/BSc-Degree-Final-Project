import { useTheme, Box, Drawer, IconButton, Paper } from "@mui/material";
import { useState } from "react";
import { Outlet } from "react-router-dom";
import RouterLink from "../Routing/RouterLink.tsx";
import SidebarSlim from "./SidebarSlim.tsx";
import { Menu } from "@mui/icons-material";

const DefaultLayout = () => {
	const theme = useTheme();
	const [width, setWidth] = useState("200px");
	const [open, setOpen] = useState(true);

	const handleOpen = () => {
		setOpen(true);
		setWidth("200px");
	};

	const handleClose = () => {
		setOpen(false);
		setWidth("40px");
	};

	return (
		<Box display="flex">
			<SidebarSlim open={!open} handleOpen={handleOpen} />
			<Drawer variant="persistent" anchor="left" open={open}>
				<Box sx={{ display: "flex", flexDirection: "column", alignItems: "center" }}>
					<Box sx={{ alignItems: "center", display: "flex", justifyContent: "flex-end", minWidth: width }}>
						<IconButton onClick={handleClose}>
							<Menu />
						</IconButton>
					</Box>
					<Box width="100%" padding={theme.spacing(1)}>
						<RouterLink path="/clips" fullWidth>
							Clips
						</RouterLink>
					</Box>
				</Box>
			</Drawer>
			<Paper
				component="main"
				elevation={0}
				sx={{
					flexGrow: 1,
					marginLeft: width,
					padding: theme.spacing(1),
					transition: theme.transitions.create("margin", {
						easing: theme.transitions.easing.easeOut,
						duration: !open ? theme.transitions.duration.enteringScreen : theme.transitions.duration.enteringScreen,
					}),
				}}
			>
				<Outlet />
			</Paper>
		</Box>
	);
};

export default DefaultLayout;
