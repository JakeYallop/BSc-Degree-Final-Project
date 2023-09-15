import { forwardRef } from "react";
import { Button, ButtonProps, Link, useTheme } from "@mui/material";
import { Link as RouterLink2, useMatch } from "react-router-dom";
import { LinkProps } from "react-router-dom";

const RouterLink = forwardRef<HTMLAnchorElement, ButtonProps & { path: string }>((props, ref) => {
	return <Button variant="outlined" component={RouterLink2} to={props.path} ref={ref} {...props} />;
});

export default RouterLink;
