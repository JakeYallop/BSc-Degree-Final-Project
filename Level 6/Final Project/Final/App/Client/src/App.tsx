import { CssBaseline, ThemeProvider, createTheme } from "@mui/material";
import { BrowserRouter, Route, Routes, redirect } from "react-router-dom";
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
			<BrowserRouter>
				<CssBaseline>
					<Routes>
						<Route path="/" element={<DefaultLayout />}>
							<Route path="/clips" element={<ClipsPage />} />
						</Route>
						<Route path="*" element={<Redirect path="/clips" />} />
					</Routes>
				</CssBaseline>
			</BrowserRouter>
		</ThemeProvider>
	);
}

export default App;

interface RedirectProps {
	path: string;
}
const Redirect = (props: RedirectProps) => {
	const { path } = props;
	redirect(path);
	return <></>;
};
