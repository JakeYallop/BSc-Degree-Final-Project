export const formatDate = (input: string | Date) => {
	var d = typeof input == "string" ? new Date(input) : input;
	return d.toLocaleString("en-US", { timeZone: "UTC" });
};
