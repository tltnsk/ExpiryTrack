<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { formatDate, formatDateTime, formatLabel, formatMoney } from '@/utils/format'

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
const isManager = computed(() => auth.user?.role === 'DepartmentManager')
const isEmployee = computed(() => auth.user?.role === 'Employee')

// only active and expiring soon items can be edited/cancelled
const isOpen = computed(() =>
  item.value?.lifecycleState === 'Active' || item.value?.lifecycleState === 'ExpiringSoon')

const canEdit = computed(() => (isManager.value || isEmployee.value) && isOpen.value)
const canCancel = computed(() => isManager.value && isOpen.value)
const canRenew = computed(() => isEmployee.value && !hasOpenRequest.value && (item.value?.lifecycleState === 'ExpiringSoon' || item.value?.lifecycleState === 'Expired'))

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

function daysBetween(from: string, to: string): number {
  const oneDay = 1000 * 60 * 60 * 24
  return Math.round((new Date(to).getTime() - new Date(from).getTime()) / oneDay)
}

const today = new Date().toISOString().slice(0, 10)

const daysLeft = computed(() => item.value ? daysBetween(today, item.value.expirationDate) : 0)


// how much of the current period has already passed
const periodPercent = computed(() => {
  if (!item.value) return 0
  const total = daysBetween(item.value.startDate, item.value.expirationDate)
  const used = daysBetween(item.value.startDate, today)
  return Math.min(100, Math.max(0, Math.round(used/total * 100)))
})

// days between the end of one period and the start of next one
const gaps = computed(() => {
  const result: number[] = []
  periods.value.forEach((period, i) => {
    const previous = periods.value[i - 1]
    if (!previous) return
    const days = daysBetween(previous.expirationDate, period.startDate) - 1
    result.push(Math.max(0, days))
  })
  return result
})

const timesRenewed = computed(() => gaps.value.length)
const lateRenewals = computed(() => gaps.value.filter(days => days > 0).length)
const daysWithoutCoverage = computed(() => gaps.value.reduce((sum, days) => sum + days, 0))

// how much the cost changed from the first period to the current one in %
const costChange = computed(() => {
  const first = periods.value[0]?.cost ?? null
  const last = periods.value.at(-1)?.cost ?? null
  if (periods.value.length < 2 || first === null || last === null || first === 0) return null
  return Math.round((last - first) / first * 100)
})

const openRequest = computed(() => requests.value.find(r => r.status !== 'Rejected' && r.status !== 'Completed') ?? null)

// expired items can be reassigned too
const canReassign = computed(() => isManager.value && item.value?.lifecycleState !== 'Cancelled')

const activeTab = ref('requests')

</script>

<template>
  <div class="page-links">
      <RouterLink to="/items" class="back-link">Back to Items</RouterLink>
  </div>

    <p v-if="loading">Loading...</p>

    <p v-else-if="errorMessage" class="error"> {{  errorMessage }}</p>

    <div v-else-if="item">
      <div class="item-layout">
        <article class="item-card">
          <div class="status-tab">
            <span class="state" :class="item.lifecycleState">{{ formatLabel(item.lifecycleState) }}</span>
          </div>

          <p>{{ item.categoryName }} | {{ item.departmentName }}</p>
          <h2 class="card-title">{{ item.name }}</h2>

          <div class="facts">
            <div>
              <span>Responsible</span>
              <strong>{{ item.responsibleUserName }}</strong>
            </div>
            <div>
              <span>Provider</span>
              <strong>{{ item.provider ?? '-' }}</strong>
            </div>
            <div>
              <span>Reference no.</span>
              <strong>{{ item.referenceNumber ?? '-' }}</strong>
            </div>
          </div>
          <p class="description">{{ item.description ?? 'No description.' }}</p>
          <div class="card-bottom">
            <span class="cost">{{ formatMoney(item.cost) }}</span>
            <div class="card-actions">
                <RouterLink v-if="canEdit" :to="`/items/${item.id}/edit`" class="card-button">Edit</RouterLink>
                <RouterLink v-if="canRenew" :to="`/items/${item.id}/renew`" class="card-button">Submit renewal</RouterLink>
                <button v-if="canCancel" class="danger" @click="cancelItem" type="button">Cancel Item</button>
            </div>
          </div>
        </article>

        <aside class="side">
          <p v-if="cancelError" class="error">{{ cancelError }}</p>
          <div class="kpis">
            <div class="kpi wide" :class="{warning: item.lifecycleState === 'ExpiringSoon', danger: item.lifecycleState === 'Expired'}">
              <p class="kpi-label">{{ daysLeft >= 0 ? 'Days left' : 'Days since it expired' }}</p>
              <p class="kpi-value">
                <template v-if="item.lifecycleState === 'Cancelled'">-</template>
                <template v-else>{{ Math.abs(daysLeft) }}<sup>days</sup></template>
              </p>
              <p class="kpi-note">{{ daysLeft >= 0 ? 'Expires' : 'Expired' }} {{ formatDate(item.expirationDate) }}</p>
            </div>

            <div class="kpi">
              <p class="kpi-label">Current period</p>
              <p class="kpi-value">{{ item.periodNumber ?? '-' }}</p>
              <p class="kpi-note">Started {{ formatDate(item.startDate) }}</p>
            </div>

            <div class="kpi">
              <p class="kpi-label">Period passed</p>
              <p class="kpi-value">{{ periodPercent }}<sup>%</sup></p>
            </div>
          </div>

          <RouterLink v-if="openRequest" :to="`/requests/${openRequest.id}`" class="open-request">
            <div>
              <p class="kpi-label">Renewal in progress</p>
              <span class="state" :class="openRequest.status">{{ formatLabel(openRequest.status) }}</span>
            </div>
          </RouterLink>
          <form v-if="canReassign" class="action-panel" @submit.prevent="reassignEmployee">
              <h3>Reassign</h3>
                <select v-model="responsibleUserId" required>
                    <option :value="null" disabled>Select a responsible employee</option>
                    <option v-if="employees.length===0" :value="null" disabled>No active employees in your department.</option>
                    <option v-for="employee in employees" :key="employee.id" :value="employee.id">
                      {{ employee.firstName }} {{  employee.lastName }}
                    </option>
                </select>
                <button type="submit">Reassign</button>
                <p v-if="reassignError" class="error">{{ reassignError }}</p>
          </form>
        </aside>

        <div v-if="periods.length > 0" class="record">
            <h3>Renewal record</h3>
            <div class="record-rows">
                <div class="record-row">
                  <span>Times renewed</span>
                  <strong>{{  timesRenewed }}</strong>
                </div>
                <div class="record-row">
                  <span>Renewed after it expired</span>
                  <strong :class="{bad: lateRenewals > 0}">{{  lateRenewals }}</strong>
                </div>
                <div class="record-row">
                  <span>Days without coverage</span>
                  <strong :class="{bad: daysWithoutCoverage > 0}">{{  daysWithoutCoverage }}</strong>
                </div>
                <div class="record-row">
                  <span>Cost since period 1</span>
                  <strong>{{ costChange === null ? '-' : (costChange > 0 ? '+' : '') + costChange + '%' }}</strong>
                </div>
            </div>
          </div>
      </div>
      <div class="tabs">
        <button type="button" :class="{active: activeTab === 'requests'}" @click="activeTab = 'requests'">
          Renewal requests <span class="count">{{ requests.length }}</span>
        </button>
        <button type="button" :class="{active: activeTab === 'periods'}" @click="activeTab = 'periods'">
          Periods <span class="count">{{ periods.length }}</span>
        </button>
        <button type="button" :class="{active: activeTab === 'history'}" @click="activeTab = 'history'">
          State history <span class="count">{{ histories.length }}</span>
        </button>
        <button type="button" :class="{active: activeTab === 'documents'}" @click="activeTab = 'documents'">
          Documents <span class="count">{{ documents.length }}</span>
        </button>
      </div>
      <div v-if="activeTab === 'requests'">
        <p v-if="requests.length === 0">No renewal requests yet.</p>
        <div v-else class="table-box">
            <table>
            <thead>
                <tr>
                  <th>Status</th>
                  <th>Proposed date</th>
                  <th>Proposed cost</th>
                  <th>Submitted</th>
                  <th></th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="request in requests" :key="request.id">
                    <td><span class="state" :class="request.status">{{ formatLabel(request.status) }}</span></td>
                    <td>{{ formatDate(request.proposedExpirationDate) }}</td>
                    <td>{{ formatMoney(request.proposedCost) }}</td>
                    <td>{{ formatDateTime(request.submittedAt)}}</td>
                    <td><RouterLink :to="`/requests/${request.id}`">Go to request</RouterLink></td>
                </tr>
            </tbody>
            </table>
        </div>
      </div>

      <div v-if="activeTab === 'periods'">
        <p v-if="periods.length === 0">No lifecycle periods yet.</p>
        <div v-else class="table-box">
            <table>
                <thead>
                    <tr>
                      <th>Period Number</th>
                      <th>Start Date</th>
                      <th>Expiration Date</th>
                      <th>Cost</th>
                      <th>Created At</th>
                      <th>Created From</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="period in periods" :key="period.periodNumber">
                        <td>{{ period.periodNumber }}</td>
                        <td>{{ formatDate(period.startDate) }}</td>
                        <td>{{ formatDate(period.expirationDate)}}</td>
                        <td>{{ formatMoney(period.cost) }}</td>
                        <td>{{ formatDateTime(period.createdAt) }}</td>
                        <td v-if="period.createdFromRequestId!==null"><RouterLink  :to="`/requests/${period.createdFromRequestId}`" >Go to request</RouterLink></td>
                        <td v-else>Initial Period</td>
                    </tr>
                </tbody>
              </table>
        </div>
      </div>

      <div v-if="activeTab === 'history'">
        <p v-if="histories.length === 0">No state history.</p>
        <div v-else class="table-box">
          <table>
                <thead>
                    <tr>
                      <th>From</th>
                      <th>To</th>
                      <th>Reason</th>
                      <th>Changed</th>
                      <th>By</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="history in histories" :key="history.id">
                        <td>{{ history.fromState ? formatLabel(history.fromState) : '-'}}</td>
                        <td>{{ formatLabel(history.toState) }}</td>
                        <td>{{ history.reason ?? '-'}}</td>
                        <td>{{ formatDateTime(history.changedAt) }}</td>
                        <td>{{ history.changedByUserName ?? 'System'}}</td>
                    </tr>
                </tbody>
          </table>
        </div>
      </div>
        <div v-if="activeTab === 'documents'">
          <p v-if="documents.length === 0">No documents.</p>
          <div v-for="doc in documents" :key="doc.id" class="file-row">
            <div class="file-info">
              <p class="file-name">{{ doc.originalFileName }}</p>
              <p>{{ Math.ceil(doc.sizeBytes / 1024) }} KB {{ doc.uploadedByUserName }} {{ formatDateTime(doc.uploadedAt) }}</p>
            </div>
            <a :href="`/api/documents/${doc.id}/download`" class="download-link">Download</a>
          </div>
        </div>
      </div>
</template>

<style scoped>
.tabs {
  display: inline-flex;
  gap: 4px;
  margin: 0 0 16px;
  padding: 4px;
  background: #f4f4f5;
  border-radius: 12px;
}

.tabs button {
  margin: 0;
  padding: 6px 12px;
  background: transparent;
  color: #6b7280;
  border-radius: 8px;
}

.tabs button.active {
  background: white;
  color: var(--text);
}

.count {
  margin-left: 4px;
  padding: 0 6px;
  border-radius: 999px;
  background: #e5e7eb;
  font-size: 0.85em;
}

.item-layout {
  display: grid;
  grid-template-columns: 3fr 2fr;
  gap: 24px;
  align-items: start;
  margin: 16px 0 32px;
}

.item-card {
  position: relative;
  overflow: hidden;
  padding: 28px 28px 0;
  color: white;
  background: #1d1d1d;
  border-radius: 28px;
}

.status-tab {
  position: absolute;
  top: 0;
  right: 0;
  padding: 0 0 12px 12px;
  background: var(--background);
  border-bottom-left-radius: 24px;
}

.status-tab::before,
.status-tab::after {
  content: '';
  position: absolute;
  width: 24px;
  height: 24px;
  background: radial-gradient(circle at bottom left, transparent 24px, var(--background) 24.5px);
}

.status-tab::before { top: 0; left: -24px; }
.status-tab::after  { bottom: -24px; right: 0; }

.status-tab .state {
  display: inline-block;
  margin: 8px 8px 0 0;
  padding: 8px 14px;
  font-weight: 600;
  border-radius: 10px;
}

.facts {
  display: flex;
  gap: 20px;
}

.facts div {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.facts div + div {
  padding-left: 20px;
}

.description {
  margin: 20px 0 24px;
  line-height: 1.6;
}

.card-bottom {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin: 0 -28px;               
  padding: 20px 28px;
  background-color: rgb(49, 47, 47);
}

.cost {
  font-size: 1.8rem;
  font-weight: 700;
}

.card-actions {
  display: flex;
  gap: 8px;
}

.card-button {
  padding: 14px 28px;
  border-radius: 999px;
  background: rgb(255, 255, 255);
  color: #000000;
  font-weight: 600;
  text-decoration: none;
}

.card-actions button {
  margin: 0;
  padding: 12px 22px;
  border-radius: 999px;
}

.card-title {
  margin: 6px 0 20px;
  color: white;
}

/* right side */
.side {
    display: flex;
    flex-direction: column;
    gap: 16px;
}

.kpis {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

.kpi {
  padding: 20px 24px;
  background: #f4f4f5;
}

.kpi.wide {
  grid-column: 1 / -1;
}

.kpi p {
  margin: 0;
}

.kpi-label {
  color: #374151;
}

.kpi-value {
  margin: 10px 0 4px;
  font-size: 2.6rem;
  line-height: 1;
}

.kpi-value sup {
  margin-left: 4px;
  font-size: 0.35em;
  vertical-align: top;
}

.kpi-note {
  color: #6b7280;
  font-size: 0.85em;
}

.kpi.warning .kpi-value { color: #d97706;}
.kpi.danger .kpi-value {color: var(--danger);}

.action-panel {
    max-width: none;
    margin: 0;
    padding: 24px;
    background: white;
    border: 1px solid #eceef1;
    border-radius: 20px;
}

.action-panel h3 {
    margin: 0 0 4px;
}

.action-panel button {
    width: 100%;
    margin: 16px 0 0;
    padding: 10px 16px;
}

.record {
  grid-column: 1/-1;
  padding: 24px;
  background: white;
  border: 1px solid #eceef1;
  border-radius: 20px;
}

.record-rows {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
}

.record-row {
  display: flex;
  flex-direction: column-reverse;
  align-items: center;
  gap: 6px;
  padding: 8px 0;
  text-align: center;
}

.record-row strong.bad {
  color: var(--danger);
}

.table-box {
    background: white;
    border: 1px solid #eceef1;
    border-radius: 12px;
    overflow: hidden;
}

.table-box table {
    margin: 0;
}

.table-box th,
.table-box td {
    padding: 14px 16px;
    border: none;
    border-bottom: 1px solid #eceef1;
    vertical-align: top;
}

.table tbody tr:last-child td {
    border-bottom: none;
}

.file-row {
    display: flex;
    align-items: center;
    gap: 14px;
    margin-bottom: 10px;
    padding: 14px 16px;
    border: 1px solid #eceef1;
    border-radius: 14px;
}

.file-info { 
    flex: 1;
}

.file-info p {
    margin: 0;
}

.file-name {
    font-weight: 500;
}

.download-link {
    padding: 8px 14px;
    border: 1px solid #d1d5db;
    border-radius: 10px;
    color: var(--text);
    text-decoration: none;
    font-weight: 500;
}

.open-request {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 18px 24px;
  border: 1px solid #eceef1;
  border-radius: 20px;
  color: var(--text);
  text-decoration: none;
}

.open-request .kpi-label {
  margin: 0 0 8px;
}

.open-request:hover {
  background: #f9fafb;
}

@media (max-width: 700px) {
  .facts { flex-direction: column; }
  .facts div + div { padding-left: 0; }
  .item-layout { grid-template-columns: 1fr;}
  .card-bottom {flex-direction: column; align-items: flex-start; gap: 12px;} 
  .record-rows {grid-template-columns: 1fr 1fr;}
}
</style>