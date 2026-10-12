import Box from '@mui/material/Box'
import Skeleton from '@mui/material/Skeleton'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'

// Grey blocks in the shape of the page, so nothing jumps when the shipment
// arrives. The text line is what screen readers hear; the blocks are hidden.
export default function TrackingSkeleton({ waybill }) {
  return (
    <Stack spacing={3}>
      <Typography role="status" color="text.secondary">
        Looking up {waybill}…
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
