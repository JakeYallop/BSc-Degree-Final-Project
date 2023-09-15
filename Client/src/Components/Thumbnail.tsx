import { styled, Box, BoxProps } from "@mui/material";
import LoadableImage, { LoadableImageProps } from "./LoadableImage.tsx";
import { Image } from "@mui/icons-material";

const ImageContainer = styled(Box, { shouldForwardProp: (prop) => prop !== "thumbWidth" })<{ thumbWidth: number }>(
	({ theme, thumbWidth }) => ({
		minWidth: `${thumbWidth}rem`,
		minHeight: `${thumbWidth}rem`,
		maxWidth: `${thumbWidth}rem`,
		maxHeight: `${thumbWidth}rem`,
		display: "flex",
	})
);

const StyledImage = styled(LoadableImage)(({ theme }) => ({
	width: "100%",
	objectFit: "cover",
}));

const StyledFallback = styled(Image)(({ theme }) => ({
	width: "100%",
	height: "100%",
	objectFit: "cover",
}));

interface ThumbnailProps extends BoxProps {
	thumbWidth?: number;
}

const Thumbnail = ({ ...otherProps }: ThumbnailProps & Omit<LoadableImageProps, keyof BoxProps>) => {
	const {
		loadingSpinnerShowDelay = 500,
		hideLoadingSpinner,
		fallbackComponent,
		displayEmpty,
		alt,
		src,
		onLoad,
		onError,
		thumbWidth,
		...boxProps
	} = otherProps;
	return (
		<ImageContainer {...boxProps} thumbWidth={thumbWidth ?? 3}>
			<StyledImage
				fallbackComponent={fallbackComponent || <StyledFallback />}
				loadingSpinnerShowDelay={loadingSpinnerShowDelay}
				displayEmpty={displayEmpty}
				alt={alt}
				src={src}
				onLoad={onLoad}
				onError={onError}
			/>
		</ImageContainer>
	);
};

export default Thumbnail;
