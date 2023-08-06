import { CssBaseline } from "@mui/material";
import { BrowserRouter, Route, Routes, redirect } from "react-router-dom";
import DefaultLayout from "./Layout/DefaultLayout.tsx";

function App() {
	return (
		<BrowserRouter>
			<CssBaseline>
				<Routes>
					<Route path="/" element={<DefaultLayout />}>
						<Route path="/clips" element={<></>} />
					</Route>
					<Route path="*" element={<Redirect path="/clips" />} />
				</Routes>
			</CssBaseline>
		</BrowserRouter>
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
