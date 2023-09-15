import { ButtonProps, Button } from "@mui/material";
import { forwardRef } from "react";
import AsyncRender from "./AsyncRender.tsx";

export interface AsyncActionButtonProps extends ButtonProps {
	loading?: boolean;
	delay?: number;
	loadingIndicator?: React.ReactElement;
	children: React.ReactChild;
}

const AsyncActionButton = forwardRef<HTMLButtonElement, AsyncActionButtonProps>(function AsyncActionButton(
	{ loading, delay = 1000, children, loadingIndicator, ...rest },
	ref
) {
	return (
		<Button ref={ref} disabled={loading} {...rest}>
			{/* {loading && showProgress ? loadingIndicator : children} */}
			<AsyncRender loading={loading} delay={delay} children={children} />
		</Button>
	);
});

export default AsyncActionButton;
