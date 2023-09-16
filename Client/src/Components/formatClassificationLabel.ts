import { formatNumber } from "./formatNumber.ts";

export interface ClassificationLabel {
	confidence: number;
	label: string;
}

export default function formatClassificationLabel(label: ClassificationLabel | null | undefined) {
	if (!label) {
		return "Unknown";
	}

	if (label.confidence < 0.75) {
		return "Unknown";
	}

	return label.label;
}

export function formatClassificationWithConfidence(
	label: ClassificationLabel | null | undefined,
	alwaysDisplay?: boolean
) {
	if (!label) {
		return "Unknown";
	}

	if (label.confidence < 0.75 && !alwaysDisplay) {
		return "Unknown";
	}

	return `${label.label} (${formatNumber(label.confidence * 100)}%)`;
}
