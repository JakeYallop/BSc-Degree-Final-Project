//modified from https://github.com/facebook/react/issues/14099#issuecomment-440013892

import { useRef, useLayoutEffect, useCallback } from "react";

function useEventCallback(fn: Function) {
	let ref = useRef<Function>();
	useLayoutEffect(() => {
		ref.current = fn;
	});
	//@ts-expect-error
	return useCallback((...args) => (0, ref.current!)(...args), []);
}

export default useEventCallback;
