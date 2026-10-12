// The format from docs/api-contract.md. The server is the authority; the
// client checks it only for quick feedback in the search box and in the mock.
export const WAYBILL_PATTERN = /^WB[0-9]{8}$/

// The API trims and uppercases too, so `wb10000004 ` finds WB10000004.
export function normalizeWaybill(value) {
  return value.trim().toUpperCase()
}
