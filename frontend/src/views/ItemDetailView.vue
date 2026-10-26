<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { formatDate, formatDateTime} from '@/utils/format'

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
    periodNumber: string
    startDate: string 
    expirationDate: string
    cost: number
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
        <span class="state" :class="item.lifecycleState">{{ item.lifecycleState }}</span>

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
              <th></th>
            </tr>
        </thead>
        <tbody>
            <tr v-for="request in requests" :key="request.id">
                <td>{{ request.status }}</td>
                <td>{{ formatDateTime(request.proposedExpirationDate) }}</td>
                <td>{{ request.submittedAt }}</td>
                <td><RouterLink :to="`/requests/${request.id}`">Go to request</RouterLink></td>
            </tr>
        </tbody>
    </table>
    </div>

</template>

<style scoped>
.state {
  padding: 2px 8px;
  border-radius: 3px;
  font-size: 0.85em;
}
.Active {
  background: #349a54;
  color: white;
}
.ExpiringSoon {
  background: #d54747;
  color: white;
}
.Expired {
  background: #fbaa29;
  color: white;
}
.Cancelled {
  background: #343131;
  color: white;
}
.error {
  color: #c53030;
}

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