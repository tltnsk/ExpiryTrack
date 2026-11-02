<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { formatDate, formatDateTime, formatLabel} from '@/utils/format'

interface Item {
    id: number
    name: string
    description: string | null
    provider: string | null
    referenceNumber: string | null
    lifecycleState: string
    categoryName: string
    departmentName: string
    responsibleUserId: number
    responsibleUserName: string
    periodNumber: number | null
    startDate: string 
    expirationDate: string
    cost: number | null
}

interface User {
    id: number
    firstName: string
    lastName: string
}

interface RenewalRequest {
    id: number
    itemId: number
    itemName: string
    status: string
    needsFinancialReview: boolean
    proposedExpirationDate: string
    proposedCost: number | null
    justification: string | null
    submittedAt: string 
    decidedAt: string | null
    completedAt: string | null
    periodId: number
    requestedByUserId: number
}

interface LifecyclePeriod {
  periodNumber: number
  startDate: string
  expirationDate: string
  cost: number | null
  createdAt: string
  createdFromRequestId: number | null
}

interface ItemStateHistory {
  id: number
  fromState: string | null
  toState: string 
  reason: string | null
  changedAt: string
  changedByUserName: string
}

interface ItemDocument {
  id: number
  originalFileName: string
  sizeBytes: number
  uploadedAt: string
  uploadedByUserName: string
}

const route = useRoute()
const auth = useAuthStore()
const id = route.params.id

const item = ref<Item | null>(null)
const loading = ref(true)
const errorMessage = ref('')
const cancelError = ref('')
const reassignError = ref('')

const employees = ref<User[]>([])
const responsibleUserId = ref<number | null>(null)


const requests = ref<RenewalRequest[]>([])
const periods = ref<LifecyclePeriod[]>([])
const histories = ref<ItemStateHistory[]>([])
const documents = ref<ItemDocument[]>([])

onMounted(async () => {
    const response = await fetch(`/api/items/${id}`)
    if (response.ok) {
        const data: Item = await response.json()
        item.value = data
        responsibleUserId.value = data.responsibleUserId
    } else if (response.status === 404 || response.status === 400) {
    errorMessage.value = 'Item not found.'
    } else {
        errorMessage.value = `Could not load item (error ${response.status}).`
    }
    loading.value = false

    // only managers can reassign, so only they need the list of employees
    if (auth.user?.role === 'DepartmentManager') {
        const employeesResponse = await fetch('/api/users/employees')
        if (employeesResponse.ok) {
            employees.value = await employeesResponse.json()
        }
    }

    const requestsResponse = await fetch(`/api/items/${id}/requests`)
    if (requestsResponse.ok) {
        requests.value = await requestsResponse.json()
    }

    const periodsResponse = await fetch(`/api/items/${id}/periods`)
    if (periodsResponse.ok) {
        periods.value = await periodsResponse.json()
    }

    const historiesResponse = await fetch(`/api/items/${id}/history`)
    if (historiesResponse.ok) {
      histories.value = await historiesResponse.json()
    }

    const documentsResponse = await fetch(`/api/items/${id}/documents`)
    if (documentsResponse.ok) {
      documents.value = await documentsResponse.json()
    }
})

const hasOpenRequest = computed(() =>requests.value.some(r => r.status !== "Rejected" && r.status !== "Completed"))


async function cancelItem() {
    cancelError.value = ''
    const result = confirm('Are you sure you want to cancel this item?')
    if (result) {
        const response = await fetch(`/api/items/${id}/cancel`, {
            method: 'PUT'
        })

        if (response.status === 200) {
            item.value = await response.json()
        } else if (response.status === 400) {
            cancelError.value = await response.text()
        } else {
            cancelError.value = `Could not cancel the item (error ${response.status})`
        }
    }
}

async function reassignEmployee() {
  reassignError.value = ''
  const response = await fetch(`/api/items/${id}/reassign`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      responsibleUserId: responsibleUserId.value
    })
  })

  if (response.status === 400) {
    const contentType = response.headers.get('Content-Type') ?? ''
    if (contentType.includes('json')) {
      const problem = await response.json()
      reassignError.value = Object.values(problem.errors).flat().join(' ')
    } else {
      reassignError.value = await response.text()
    }
    return
  }
  if (!response.ok) {
    reassignError.value = `Could not reassign the item (error ${response.status}).`
    return
  }

  // the response is the updated item, so the page shows the new responsible employee
  item.value = await response.json()
}

</script>

<template>
  <div class="page-links">
      <RouterLink to="/items">Back to Items</RouterLink>
      <RouterLink v-if="item && (auth.user?.role === 'DepartmentManager' || auth.user?.role === 'Employee') && (item.lifecycleState === 'Active' || item.lifecycleState === 'ExpiringSoon')" :to="`/items/${item.id}/edit`">Edit Item</RouterLink>
  </div>

    <p v-if="loading">Loading...</p>

    <p v-else-if="errorMessage" class="error"> {{  errorMessage }}</p>

    <div v-else-if="item">
        <h2>{{ item.name }}</h2>
        <span class="state" :class="item.lifecycleState">{{ formatLabel(item.lifecycleState) }}</span>

        <h3>Details</h3>
        <dl>
            <dt>Description</dt>
            <dd>{{ item.description ?? '-' }}</dd>
            <dt>Category</dt>
            <dd>{{ item.categoryName }}</dd>
            <dt>Department</dt>
            <dd>{{ item.departmentName }}</dd>
            <dt>Responsible</dt>
            <dd>{{ item.responsibleUserName }}</dd>
            <dt>Provider</dt>
            <dd>{{ item.provider ?? '-' }}</dd>
            <dt>Reference Number</dt>
            <dd>{{ item.referenceNumber ?? '-' }}</dd>
        </dl>

        <h3>Current Period</h3>
        <dl>
            <dt>Period</dt>
            <dd>{{ item.periodNumber }}</dd>
            <dt>Start date</dt>
            <dd>{{ formatDate(item.startDate) }}</dd>
            <dt>Expiration Date</dt>
            <dd>{{ formatDate(item.expirationDate) }}</dd>
            <dt>Cost</dt>
            <dd>{{ item.cost ?? '-' }}</dd>
        </dl>

        <button v-if="auth.user?.role === 'DepartmentManager' && (item.lifecycleState === 'Active' || item.lifecycleState === 'ExpiringSoon')"  @click="cancelItem">Cancel Item</button>
        <p v-if="cancelError">{{ cancelError }}</p>
        
        <form v-if="auth.user?.role === 'DepartmentManager' && (item.lifecycleState === 'Active' || item.lifecycleState === 'ExpiringSoon')" @submit.prevent="reassignEmployee">
            <div>
                <label>
                    Reassign Employee
                    <select v-model="responsibleUserId" required>
                        <option :value="null" disabled>Select a responsible employee</option>
                        <option v-if="employees.length===0" :value="null" disabled>No active employees in your department.</option>
                        <option v-for="employee in employees" :key="employee.id" :value="employee.id">
                          {{ employee.firstName }} {{  employee.lastName }}
                        </option>
                    </select>
                </label>
            </div>
            <button type="submit">Reassign employee</button>
            <p v-if="reassignError" class="error">{{ reassignError }}</p>
        </form>
        <RouterLink v-if="auth.user?.role=='Employee' && (item.lifecycleState=='ExpiringSoon' || item.lifecycleState === 'Expired') && !hasOpenRequest" :to="`/items/${item.id}/renew`">Submit a Renewal Request</RouterLink>

        <h2>Renewal Requests</h2>
        <p v-if="requests.length === 0">No renewal requests yet.</p>
        <table v-else>
        <thead>
            <tr>
              <th>Status</th>
              <th>Proposed Date</th>
              <th>Submitted</th>
            </tr>
        </thead>
        <tbody>
            <tr v-for="request in requests" :key="request.id">
                <td><span class="state" :class="request.status">{{ formatLabel(request.status) }}</span></td>
                <td>{{ formatDate(request.proposedExpirationDate) }}</td>
                <td>{{ formatDateTime(request.submittedAt)}}</td>
                <td><RouterLink :to="`/requests/${request.id}`">Go to request</RouterLink></td>
            </tr>
        </tbody>
        </table>

        <h2>Lifecycle Periods</h2>
        <p v-if="periods.length === 0">No lifecycle periods yet.</p>
        <table v-else>
        <thead>
            <tr>
              <th>Period Number</th>
              <th>Start Date</th>
              <th>Expiration Date</th>
              <th>Cost</th>
              <th>Created At</th>
              <th>Created From Request</th>
            </tr>
        </thead>
        <tbody>
            <tr v-for="period in periods" :key="period.periodNumber">
                <td>{{ period.periodNumber }}</td>
                <td>{{ formatDate(period.startDate) }}</td>
                <td>{{ formatDate(period.expirationDate)}}</td>
                <td>{{ period.cost ?? "-" }}</td>
                <td>{{ formatDateTime(period.createdAt) }}</td>
                <td v-if="period.createdFromRequestId!==null"><RouterLink  :to="`/requests/${period.createdFromRequestId}`" >Go to request</RouterLink></td>
                <td v-else>Initial Period - No request</td>
            </tr>
        </tbody>
      </table>

      <h2>Item State History</h2>
        <p v-if="histories.length === 0">No item state history.</p>
        <table v-else>
        <thead>
            <tr>
              <th>From State</th>
              <th>To State</th>
              <th>Reason</th>
              <th>Changed At</th>
              <th>Changed By</th>
            </tr>
        </thead>
        <tbody>
            <tr v-for="history in histories" :key="history.id">
                <td>{{ formatLabel(history.fromState)}} </td>
                <td>{{ formatLabel(history.toState)}}</td>
                <td>{{ history.reason}}</td>
                <td>{{ formatDateTime(history.changedAt) }}</td>
                <td>{{ history.changedByUserName}}</td>
            </tr>
        </tbody>
      </table>

       <h2>Documents</h2>
        <p v-if="documents.length === 0">No documents.</p>
        <table v-else>
        <thead>
            <tr>
              <th>File</th>
              <th>Uploaded At</th>
              <th>Uploaded By</th>
            </tr>
        </thead>
        <tbody>
            <tr v-for="doc in documents" :key="doc.id">
                <td><a :href="`/api/documents/${doc.id}/download`">{{ doc.originalFileName }}</a></td>
                <td>{{ formatDateTime(doc.uploadedAt)}}</td>
                <td>{{ doc.uploadedByUserName}}</td>
            </tr>
        </tbody>
      </table>
    </div>

</template>

<style scoped>
form {
  max-width: 420px;
  margin-left: 0;
}

form > button {
  padding: 4px 4px;
  margin-right: 8px;
  background: var(--navy);
  color: white;
  border: none;
  border-radius: 10px;
  font: inherit;
  font-weight: 400;
  cursor: pointer;
  width: 40%;
}
</style>