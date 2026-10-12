import Box from '@mui/material/Box'
import Skeleton from '@mui/material/Skeleton'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { useEffect, useState } from 'react'

// A normal lookup takes well under a second, so a wait this long almost
// always means the free API on Render is starting up after sleeping.
const WAKING_UP_AFTER_MS = 3000

// Grey blocks in the shape of the page, so nothing jumps when the shipment
// arrives. The text line is what screen readers hear; the blocks are hidden.
// The timer starts when the skeleton appears, so the parent gives it a new
// key for each lookup.
export default function TrackingSkeleton({ waybill }) {
  const [isWakingUp, setIsWakingUp] = useState(false)

  useEffect(() => {
    const timer = setTimeout(() => setIsWakingUp(true), WAKING_UP_AFTER_MS)
    return () => clearTimeout(timer)
  }, [])

  return (
    <Stack spacing={3}>
      <Typography role="status" color="text.secondary">
        Looking up {waybill}…
        {isWakingUp && ' The server has been asleep and is waking up. This can take up to a minute.'}
      </Typography>
      <Stack spacing={3} aria-hidden="true">
        <Skeleton variant="rounded" height={120} />
        <Box sx={{ display: 'grid', gap: 3, gridTemplateColumns: { md: '1fr 1fr' } }}>
          <Skeleton variant="rounded" height={200} />
          <Skeleton variant="rounded" height={200} sx={{ display: { xs: 'none', md: 'block' } }} />
        </Box>
        <Skeleton variant="rounded" height={260} />
      </Stack>
    </Stack>
  )
}
