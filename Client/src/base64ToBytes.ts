// modified from: https://developer.mozilla.org/en-US/docs/Glossary/Base64#the_unicode_problem
// to support base64Url format
// See here for differences - 2 characters are mapped differently between the specifications
// https://en.wikipedia.org/wiki/Base64#Variants_summary_table
export default function base64urlToBytes(base64url: string) {
	const binString = atob(base64url.replace(/-/g, "+").replace(/_/g, "/"));
	// @ts-ignore
	return Uint8Array.from(binString, (m) => m.codePointAt(0));
}
