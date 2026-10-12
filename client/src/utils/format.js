// Built once and reused: creating an Intl formatter is far slower than using one.
const dateTimeFormat = new Intl.DateTimeFormat('en-ZA', {
  timeZone: 'Africa/Johannesburg',
  dateStyle: 'medium',
  timeStyle: 'short',
})

// A date-only string like "2026-10-09" parses as midnight UTC, so formatting
// it in UTC keeps the same calendar day whatever time zone it's shown in.
const dateOnlyFormat = new Intl.DateTimeFormat('en-ZA', {
  timeZone: 'UTC',
  dateStyle: 'medium',
})

/** An API timestamp (UTC) shown in South African time, e.g. "08 Oct 2026, 07:42". */
export function formatDateTime(isoTimestamp) {
  return dateTimeFormat.format(new Date(isoTimestamp))
}

/** A date with no time, e.g. "2026-10-09" → "09 Oct 2026". */
export function formatDate(isoDate) {
  return dateOnlyFormat.format(new Date(isoDate))
}
