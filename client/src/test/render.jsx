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
// fake answers min-width and max-width queries as a screen of `width` pixels
// would, which is all MUI's useMediaQuery asks.
export function setScreenWidth(width) {
  vi.stubGlobal(
    'matchMedia',
    vi.fn((query) => ({
      matches: matchesWidth(query, width),
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

function matchesWidth(query, width) {
  const min = query.match(/min-width:\s*([\d.]+)px/)
  const max = query.match(/max-width:\s*([\d.]+)px/)
  return (!min || width >= Number(min[1])) && (!max || width <= Number(max[1]))
}
