import { describe, expect, it } from 'vitest'
import { formatDate, formatDateTime } from './format.js'

describe('formatDateTime', () => {
  it('shows a UTC timestamp in Johannesburg time', () => {
    expect(formatDateTime('2026-10-08T05:42:00+00:00')).toBe('08 Oct 2026, 07:42')
  })

  it('moves to the next day when Johannesburg has passed midnight', () => {
    expect(formatDateTime('2026-10-07T23:30:00+00:00')).toBe('08 Oct 2026, 01:30')
  })
})

describe('formatDate', () => {
  it('shows a date-only value on the same calendar day', () => {
    expect(formatDate('2026-10-09')).toBe('09 Oct 2026')
  })
})
