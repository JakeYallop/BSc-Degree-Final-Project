export const formatNumber = (input: number, options?: Intl.NumberFormatOptions) => {
	return input.toLocaleString(undefined, {
		maximumFractionDigits: 4,
		maximumSignificantDigits: 4,
		notation: "standard",
		...options,
	});
};
