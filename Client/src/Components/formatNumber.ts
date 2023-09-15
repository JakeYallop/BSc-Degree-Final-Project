export const formatNumber = (input: number, options?: Intl.NumberFormatOptions) => {
	return input.toLocaleString(undefined, {
		maximumFractionDigits: 2,
		minimumFractionDigits: 2,
		notation: "standard",
		...options,
	});
};
