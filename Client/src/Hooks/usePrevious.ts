import { useState } from "react";

function usePrevious(value: any) {
	const [current, setCurrent] = useState<any>(value);
	const [previous, setPrevious] = useState<any>(null);

	if (value !== current) {
		setPrevious(current);
		setCurrent(value);
	}

	return previous;
}

export default usePrevious;
