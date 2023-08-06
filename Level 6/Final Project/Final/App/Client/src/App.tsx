import { useState } from "react";
import RouterLink from "./Routing/RouterLink.tsx";
import { AppBar, Box, CssBaseline, Drawer, IconButton, List, ListItem, Paper, styled, useTheme } from "@mui/material";
import { BrowserRouter, Outlet, Route, Routes, redirect } from "react-router-dom";
import { Menu as MenuIcon, ThreeMp } from "@mui/icons-material";

function App() {
	return (
		<BrowserRouter>
			<CssBaseline>
				<Routes>
					<Route path="/" element={<DefaultLayout />}>
						<Route path="/clips" element={<Test />} />
					</Route>
					<Route path="*" element={<Redirect path="/clips" />} />
				</Routes>
			</CssBaseline>
		</BrowserRouter>
	);
}

export default App;

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
							<MenuIcon />
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

const Test = () => "Clips!!";

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
				<MenuIcon />
			</IconButton>
		</Box>
	);
};

interface RedirectProps {
	path: string;
}
const Redirect = (props: RedirectProps) => {
	const { path } = props;
	redirect(path);
	return <></>;
};
