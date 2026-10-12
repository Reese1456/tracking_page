import { ErrorOutline as ErrorOutlineIcon } from '@mui/icons-material'
import Box from '@mui/material/Box'
import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { alpha } from '@mui/material/styles'
import { formatDateTime } from '../utils/format.js'

export default function TrackingHistory({ events }) {
  return (
    <Card variant="outlined">
      <CardContent>
        <Typography variant="h6" component="h2">
          Tracking history
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Newest first. Times are South African Standard Time (SAST).
        </Typography>
        <Stack component="ol" aria-label="Tracking history" spacing={1} sx={{ listStyle: 'none', m: 0, p: 0 }}>
          {/* Events have no id, and the list is only ever replaced whole,
              never reordered, so the position is a stable key. */}
          {events.map((event, index) => (
            <HistoryItem key={index} event={event} />
          ))}
        </Stack>
      </CardContent>
    </Card>
  )
}

function HistoryItem({ event }) {
  const isException = event.status === 'Exception'

  return (
    <Box
      component="li"
      sx={{
        borderLeft: '4px solid',
        borderLeftColor: isException ? 'error.main' : 'divider',
        bgcolor: isException ? (theme) => alpha(theme.palette.error.main, 0.08) : 'transparent',
        borderRadius: 1,
        px: 2,
        py: 1.5,
      }}
    >
      <Stack direction="row" spacing={1} alignItems="flex-start">
        {isException && <ErrorOutlineIcon color="error" titleAccess="Exception" sx={{ mt: 0.25 }} />}
        <div>
          <Typography fontWeight={500} color={isException ? 'error' : 'text.primary'}>
            {event.description}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            <time dateTime={event.timestamp}>{formatDateTime(event.timestamp)}</time> · {event.location}
          </Typography>
        </div>
      </Stack>
    </Box>
  )
}
