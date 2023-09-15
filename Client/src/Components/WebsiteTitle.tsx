import { Portal } from "@mui/material";
import { useEffect, useRef } from "react";
interface WebsiteTitleProps {
    children: string;
}

const WebsiteTitle = (props: WebsiteTitleProps) => {
    const { children } = props;
    const portalRef = useRef(document.createElement("title"));

    useEffect(() => {
        const current = portalRef.current;
        document.head.prepend(current);
        return () => current.remove();
    }, []);

    if (!portalRef.current) return null;
    return <Portal container={portalRef.current}>{children}</Portal>;
};

export default WebsiteTitle;
