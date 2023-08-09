import { styled, CircularProgress, Box } from "@mui/material";
import React, { ImgHTMLAttributes, useState, useEffect, ReactEventHandler } from "react";
import useTimeout from "../Hooks/useTimeout.tsx";

export interface LoadableImageProps extends ImgHTMLAttributes<HTMLImageElement> {
	loadingSpinnerShowDelay?: number;
	hideLoadingSpinner?: string;
	fallbackComponent?: React.ReactNode;
	displayEmpty?: boolean;
}

const LoadingContainer = styled("div")(({ theme }) => ({
	display: "flex",
	justifyContent: "center",
	alignItems: "center",
	height: "100%",
	padding: theme.spacing(1),
}));

const LoadableImage = ({
	loadingSpinnerShowDelay = 500,
	hideLoadingSpinner,
	fallbackComponent,
	displayEmpty,
	className,
	alt,
	src,
	onLoad,
	onError,
	...imageProps
}: LoadableImageProps) => {
	//if falsy, default show to true
	const [show, setShow] = useState(!loadingSpinnerShowDelay);
	const [loaded, setLoaded] = useState(false);
	const [errored, setErrored] = useState(false);

	const delayShow = () => {
		if (loadingSpinnerShowDelay) {
			setShow(true);
		}
	};
	useTimeout(delayShow, (!show && loadingSpinnerShowDelay) || null);

	useEffect(() => {
		setLoaded(false);
		setErrored(false);
	}, [src]);

	const hasValidSrc = src || (src === "" && displayEmpty);

	if (!hasValidSrc && !React.isValidElement(fallbackComponent)) {
		return null;
	}

	const handleLoaded: ReactEventHandler<HTMLImageElement> = (event) => {
		setLoaded(true);
		if (onLoad) {
			onLoad(event);
		}
	};

	const handleError: ReactEventHandler<HTMLImageElement> = (event) => {
		setErrored(true);
		setLoaded(true);
		if (onError) {
			onError(event);
		}
	};

	if ((errored || !hasValidSrc) && React.isValidElement(fallbackComponent)) {
		return fallbackComponent;
	}

	return (
		<>
			{!loaded && show && (
				<LoadingContainer>
					<CircularProgress size="1.5rem" />
				</LoadingContainer>
			)}
			<Box
				component="img"
				sx={{
					...(!loaded && {
						display: "none",
					}),
					width: "100%",
				}}
				className={className}
				src={src}
				alt={alt || "map icon"}
				onLoad={handleLoaded}
				onError={handleError}
				{...imageProps}
			/>
		</>
	);
};

export default LoadableImage;
