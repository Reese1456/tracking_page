import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'

// React Testing Library only cleans up by itself when the runner's globals
// are on; these tests import describe, it and expect explicitly instead.
afterEach(cleanup)
