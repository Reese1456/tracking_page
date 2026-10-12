import Card from '@mui/material/Card'
import CardContent from '@mui/material/CardContent'
import Chip from '@mui/material/Chip'
import Typography from '@mui/material/Typography'
import { getSteps } from '../constants/steps.js'
import { formatDate, formatDateTime } from '../utils/format.js'
import DetailList from './DetailList.jsx'

function statusColor({ isException, status }) {
  if (isException) {
    return 'error'
  }
  return status === 'Delivered' ? 'success' : 'primary'
}

export default function ShipmentSummary({ shipment }) {
  // The stepper's label for the API's step, so the chip and the stepper
  // always use the same words.
  const statusLabel = getSteps(shipment.isException)[shipment.stepIndex].label

  return (
    <Card variant="outlined" sx={{ height: '100%' }}>
      <CardContent>
        <Typography variant="h6" component="h2" gutterBottom>
          Shipment
        </Typography>
        <DetailList
          items={[
            { label: 'Waybill', value: shipment.waybill },
            {
              label: 'Status',
              value: <Chip label={statusLabel} color={statusColor(shipment)} size="small" component="span" />,
            },
            { label: 'Estimated delivery', value: formatDate(shipment.estimatedDelivery) },
            { label: 'Last updated', value: formatDateTime(shipment.lastUpdated) },
          ]}
        />
      </CardContent>
    </Card>
  )
}
