import { CircularProgressProps, CircularProgress } from "@mui/material";
import { ReactNode, useState, cloneElement, useEffect } from "react";
import useTimeout from "../Hooks/useTimeout.tsx";

type AsyncRenderProps<LoadingIndicatorProps extends {} = CircularProgressProps> = {
	[K in keyof LoadingIndicatorProps]: LoadingIndicatorProps[K];
} & {
	loading?: boolean;
	delay?: number;
	fallback?: React.ReactElement;
	children: ReactNode | ReactNode[];
};

export const DefaultBusyIndicator = <CircularProgress size={24} />;
const AsyncRender = (props: AsyncRenderProps) => {
	const [showProgress, setShowProgress] = useState(false);
	const { loading, delay = 500, children, fallback: Fallback, ...rest } = props;
	const component = Fallback === null || Fallback ? Fallback : DefaultBusyIndicator;
	const componentWithProps = cloneElement(component, rest);
	useEffect(() => {
		if (!loading) {
			setShowProgress(false);
		}
	}, [loading]);

	useTimeout(
		() => {
			if (loading) {
				setShowProgress(true);
			}
		},
		delay || 1,
		[loading]
	);

	if (loading && showProgress) {
		return <>{componentWithProps}</>;
	} else if (loading && !showProgress) {
		return <>{children}</>;
	} else {
		return <>{children}</>;
	}
};

export default AsyncRender;
