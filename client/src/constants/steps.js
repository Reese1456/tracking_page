// Icons come from the package root, which is its ES module build. The
// per-icon paths (@mui/icons-material/CheckCircle) are CommonJS, and Vite 8
// hands this "type": "module" app their whole exports object instead of the
// component, so the page renders blank.
import {
  CheckCircle as CheckCircleIcon,
  DeliveryDining as DeliveryDiningIcon,
  ErrorOutline as ErrorOutlineIcon,
  Inventory2 as Inventory2Icon,
  LocalShipping as LocalShippingIcon,
  Settings as SettingsIcon,
} from '@mui/icons-material'

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
