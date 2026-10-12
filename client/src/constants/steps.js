import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import DeliveryDiningIcon from '@mui/icons-material/DeliveryDining'
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline'
import Inventory2Icon from '@mui/icons-material/Inventory2'
import LocalShippingIcon from '@mui/icons-material/LocalShipping'
import SettingsIcon from '@mui/icons-material/Settings'

// Positions match the API's stepIndex (0-4). The client highlights the step
// the API names and never works the index out from the status.
export const STEPS = [
  { label: 'Order Packed', icon: Inventory2Icon },
  { label: 'Processing', icon: SettingsIcon },
  { label: 'In Transit', icon: LocalShippingIcon },
  { label: 'Out for Delivery', icon: DeliveryDiningIcon },
  { label: 'Delivered', icon: CheckCircleIcon },
]

export const EXCEPTION_STEP = { label: 'Delivery Exception', icon: ErrorOutlineIcon }

export function getSteps(isException) {
  return isException ? [...STEPS.slice(0, -1), EXCEPTION_STEP] : STEPS
}
