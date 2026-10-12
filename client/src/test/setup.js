import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { format } from 'node:util'
import { afterEach, beforeEach, expect, vi } from 'vitest'

// React reports problems such as duplicate keys only through console.error,
// so without this a test with a broken page could still pass.
let consoleErrors
beforeEach(() => {
  consoleErrors = []
  vi.spyOn(console, 'error').mockImplementation((...args) => consoleErrors.push(format(...args)))
})

// One hook, in this order: a failed check would skip any later hook, and a
// page left mounted would break every test after it. React Testing Library
// only cleans up by itself when the runner's globals are on; these tests
// import describe, it and expect explicitly instead.
afterEach(() => {
  cleanup()
  console.error.mockRestore()
  expect(consoleErrors, 'console.error was called').toEqual([])
})
