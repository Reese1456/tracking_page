import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { useParams } from 'react-router'
import DeliveryStepper from '../components/DeliveryStepper.jsx'
import WaybillSearch from '../components/WaybillSearch.jsx'
import { useTracking } from '../hooks/useTracking.js'

export default function TrackingPage() {
  const { waybill } = useParams()
  const { state, data } = useTracking(waybill)

  return (
    <Stack spacing={3}>
      {/* A new key resets the box to the new waybill after a search. */}
      <WaybillSearch key={waybill} initialValue={waybill} showDemoWaybills={false} />
      <Typography variant="h4" component="h1">
        Waybill {waybill}
      </Typography>
      {state === 'loading' && <Typography>Loading…</Typography>}
      {state === 'success' && (
        <Paper variant="outlined" sx={{ p: { xs: 2, md: 3 } }}>
          <DeliveryStepper shipment={data} />
        </Paper>
      )}
    </Stack>
  )
}
