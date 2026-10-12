import { green, grey, red } from '@mui/material/colors'
import { createTheme } from '@mui/material/styles'

// Built in two passes so the second can use the first's breakpoints and
// augmentColor, the pattern MUI documents for themes that refer to themselves.
const baseTheme = createTheme({
  palette: {
    success: { main: green[800] },
    error: { main: red[700] },
  },
})

const { breakpoints, palette } = baseTheme

export const theme = createTheme(baseTheme, {
  palette: {
    // Pending step labels are grey text, which needs 4.5:1 contrast on white:
    // grey[600] gives 4.6:1, grey[500] only 2.7:1.
    pending: palette.augmentColor({ color: { main: grey[600] }, name: 'pending' }),
  },
  // Larger on phones, smaller from md up. md is also where the stepper turns
  // horizontal, so text and layout change at the same width.
  typography: {
    subtitle1: { fontSize: '1.25rem', [breakpoints.up('md')]: { fontSize: '1.125rem' } },
    body1: { fontSize: '1.125rem', [breakpoints.up('md')]: { fontSize: '1rem' } },
    body2: { fontSize: '1rem', [breakpoints.up('md')]: { fontSize: '0.875rem' } },
  },
})
