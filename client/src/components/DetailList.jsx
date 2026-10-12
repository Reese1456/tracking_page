import Box from '@mui/material/Box'
import Typography from '@mui/material/Typography'

// A <dl> of label/value pairs, so screen readers announce each value with
// its label. HTML allows a <div> around each <dt>/<dd> pair.
export default function DetailList({ items }) {
  return (
    <Box component="dl" sx={{ m: 0, display: 'grid', gap: 1.5 }}>
      {items.map(({ label, value }) => (
        <div key={label}>
          <Typography component="dt" variant="body2" color="text.secondary">
            {label}
          </Typography>
          <Typography component="dd" sx={{ m: 0 }}>
            {value}
          </Typography>
        </div>
      ))}
    </Box>
  )
}
