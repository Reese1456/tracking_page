import { ThemeProvider } from '@mui/material/styles'
import { render } from '@testing-library/react'
import { vi } from 'vitest'
import { MemoryRouter } from 'react-router'
import { theme } from '../theme.js'

// Renders with the app's theme and an in-memory router starting at `route`,
// so components see the same colours and routing as in the browser.
export function renderWithProviders(ui, { route = '/' } = {}) {
  return render(
    <ThemeProvider theme={theme}>
      <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
    </ThemeProvider>,
  )
}

// jsdom has no layout engine, so it has no window.matchMedia at all. This
// fake answers min-width queries, the only kind theme.breakpoints.up() makes,
// as a screen of `width` pixels would. The screen never resizes during a test.
export function setScreenWidth(width) {
  vi.stubGlobal(
    'matchMedia',
    vi.fn((query) => ({
      matches: width >= minWidth(query),
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    })),
  )
}

// Fails loudly on any other query, so a new kind of media query in the app
// can't be silently answered wrong.
function minWidth(query) {
  const match = query.match(/^\(min-width:\s*([\d.]+)px\)$/)
  if (!match) {
    throw new Error(`setScreenWidth can't answer the media query ${query}`)
  }
  return Number(match[1])
}
