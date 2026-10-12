import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import WaybillSearch from '../components/WaybillSearch.jsx'

export default function SearchPage() {
  return (
    <Stack spacing={2} sx={{ maxWidth: 640, mx: 'auto', mt: { md: 4 } }}>
      <Typography variant="h4" component="h1">
        Track your parcel
      </Typography>
      <Typography color="text.secondary">Enter the waybill number from your shipping confirmation.</Typography>
      <WaybillSearch />
    </Stack>
  )
}
