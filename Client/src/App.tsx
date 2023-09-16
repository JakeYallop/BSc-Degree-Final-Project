import { CssBaseline, ThemeProvider, createTheme } from "@mui/material";
import { BrowserRouter, Navigate, Route, Routes, redirect, useNavigate } from "react-router-dom";
import DefaultLayout from "./Components/Layout/DefaultLayout.tsx";
import ClipsPage from "./Components/Clips/ClipsPage.tsx";

const theme = createTheme({
	palette: {
		mode: "dark",
	},
	components: {
		MuiStack: {
			defaultProps: {
				useFlexGap: true,
			},
		},
	},
});

function App() {
	return (
		<ThemeProvider theme={theme}>
			<CssBaseline>
				<BrowserRouter>
					<Routes>
						<Route path="/" element={<DefaultLayout />}>
							<Route path="/clips" element={<ClipsPage />} />
							<Route path="/" element={<Navigate to="/clips" replace />} />
						</Route>
						<Route path="/" element={<Navigate to="/clips" replace />} />
					</Routes>
				</BrowserRouter>
			</CssBaseline>
		</ThemeProvider>
	);
}

export default App;
