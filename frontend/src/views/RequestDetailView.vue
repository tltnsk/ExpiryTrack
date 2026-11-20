<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { formatDate, formatDateTime, formatLabel, formatMoney} from '@/utils/format'

interface RequestAction{
    type: string
    fromStatus: string | null
    toStatus: string
    recommendation: string | null
    comment: string | null
    createdAt: string
    actorUserId: number | null
    actorUserName: string
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
    actions: RequestAction[]
}


interface RequestDocument {
  id: number
  originalFileName: string
  sizeBytes: number
  uploadedAt: string
  uploadedByUserName: string
}

const route = useRoute()
const auth = useAuthStore()
const id = route.params.id

const request = ref<RenewalRequest | null>(null)

const errorMessage = ref('')
const loading = ref(true)

// finance
const recommendation = ref('')
const comment = ref('')
const financeError = ref('')
const financeSaving = ref(false)
const canFinanceReview = computed(() => auth.user?.role === 'FinanceOfficer' && request.value?.status === 'PendingFinancialReview')

// manager
const canManagerReview = computed(() => auth.user?.role === 'DepartmentManager' && request.value?.status === 'PendingManagerApproval')
const canManagerReject = computed(() => auth.user?.role === 'DepartmentManager' && (request.value?.status === 'PendingManagerApproval' || request.value?.status === 'ClarificationRequired' || (request.value?.status === 'Approved' && request.value.proposedExpirationDate <= new Date().toISOString().slice(0, 10))))
const managerError = ref('')
const managerSaving = ref(false)

// employee
const canAnswerClarification = computed(() => auth.user?.role === 'Employee' && request.value?.status === 'ClarificationRequired')
const canRecordCompletion = computed(() => auth.user?.role === 'Employee' && request.value?.status === 'Approved')
const employeeError = ref('')
const employeeSaving = ref(false)
const employeeComment = ref('')
const employeeCompleteSaving = ref(false)
const employeeCompleteError = ref('')


const documents = ref<RequestDocument[]>([])

async function loadRequest() {
    const response = await fetch(`/api/requests/${id}`)
    if (response.ok) {
        const data: RenewalRequest = await response.json()
        request.value = data
    } else if (response.status === 404 || response.status === 400) {
        errorMessage.value = 'Request not found.'
    } else {
        errorMessage.value = `Could not load request (error ${response.status}).`
    }
    loading.value = false
}

async function financeRecommendation() {
    financeError.value = ''
    financeSaving.value = true
    const response = await fetch(`/api/requests/${id}/finance-review`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            recommendation: recommendation.value,
            comment: comment.value
        })
    })

    financeSaving.value = false
    if (response.status === 400) {
        const contentType = response.headers.get('Content-Type') ?? ''
        if (contentType.includes('json')) {
            const problem = await response.json()
            financeError.value = Object.values(problem.errors).flat().join(' ')
        } else {
            financeError.value = await response.text()
        }
        return
    }
    if (!response.ok) {
        financeError.value = `Could not send recommendation (error ${response.status}).`
        return
    }

    await loadRequest()
}

const managerComment = ref('')

async function managerAction(action: string) {
    managerError.value = ''
    managerSaving.value = true
    const response = await fetch(`/api/requests/${id}/${action}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            comment: managerComment.value
        })
    })

    managerSaving.value = false
    if (response.status === 400) {
        const contentType = response.headers.get('Content-Type') ?? ''
        if (contentType.includes('json')) {
            const problem = await response.json()
            managerError.value = Object.values(problem.errors).flat().join(' ')
        } else {
            managerError.value = await response.text()
        }
        return
    }
    if (!response.ok) {
        managerError.value = `Could not send decision (error ${response.status}).`
        return
    }

    await loadRequest()
}

async function provideClarification() {
    employeeError.value = ''
    employeeSaving.value = true

    const response = await fetch(`/api/requests/${id}/provide-clarification`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            answer: employeeComment.value
        })
    })

    employeeSaving.value = false
    if (response.status === 400) {
        const contentType = response.headers.get('Content-Type') ?? ''
        if (contentType.includes('json')) {
            const problem = await response.json()
            employeeError.value = Object.values(problem.errors).flat().join(' ')
        } else {
            employeeError.value = await response.text()
        }
        return
    }
    if (!response.ok) {
        employeeError.value = `Could not send answer (error ${response.status}).`
        return
    }

    await loadRequest()
}

async function completeRenewal() {
    employeeCompleteError.value = ''
    employeeCompleteSaving.value = true

    const response = await fetch(`/api/requests/${id}/complete-renewal`, {
        method: 'POST',
    })

    employeeCompleteSaving.value = false
    if (response.status === 400) {
        const contentType = response.headers.get('Content-Type') ?? ''
        if (contentType.includes('json')) {
            const problem = await response.json()
            employeeCompleteError.value = Object.values(problem.errors).flat().join(' ')
        } else {
            employeeCompleteError.value = await response.text()
        }
        return
    }
    if (!response.ok) {
        employeeCompleteError.value = `Could not complete (error ${response.status}).`
        return
    }

    await loadRequest()
}

// find clarifictions from history
const clarificationUsed = computed(() => request.value?.actions.some(a => a.type === 'ClarificationRequested') ?? false)

onMounted(async () => {
    await loadRequest()

    const documentsResponse = await fetch(`/api/requests/${id}/documents`)
    if (documentsResponse.ok) {
      documents.value = await documentsResponse.json()
    }
})


// the steps of the request
const steps = computed(() => {
    if (!request.value) return []
    const list = ['Submitted']
    if (request.value.needsFinancialReview) list.push('Financial review')
    list.push('Manager decision', 'Renewal completed')
    return list
})

// how many steps are finished when the request has this status
function stepsDoneFor(status: string | null): number {
    const total = steps.value.length
    if (status === 'Completed') return total
    if (status === 'Approved') return total - 1
    if (status === 'PendingManagerApproval' || status === 'ClarificationRequired') return total - 2
    if (status === 'PendingFinancialReview') return 1
    return 0
}

const isRejected = computed(() => request.value?.status === 'Rejected')

const doneSteps = computed(() => {
    if (!request.value) return 0
    if (isRejected.value) {
        const last = request.value.actions[request.value.actions.length - 1]
        return stepsDoneFor(last?.fromStatus ?? null)
    }
    return stepsDoneFor(request.value.status)
})

function stepClass(index: number): string {
    const status = request.value?.status
    if (index < doneSteps.value) {
        if (status === 'Completed' && index === steps.value.length - 1) return 'success'
        return 'done'
    }
    if (index === doneSteps.value) {
        if (isRejected.value) return 'stopped'
        if (status === 'ClarificationRequired') return 'warning'
        return 'current'
    }
    return 'pending'
}

// the symbol inside each circle
const stepIcons: Record<string, string> = {
    done: '✓', success: '✓', stopped: '✕', warning: '', current: '•', pending: ''
}

const clarificationQuestion = computed(() =>
    request.value?.actions.find(a => a.type === 'ClarificationRequested')?.comment ?? null
)
</script>

<template>
    <RouterLink to="/requests" class="back-link">Back to Renewal Requests</RouterLink>

    <p v-if="loading">Loading...</p>

    <p v-else-if="errorMessage" class="error"> {{  errorMessage }}</p>
    <div v-else-if="request">
        <ol class="stepper">
            <li v-for="(step, index) in steps" :key="step" :class="stepClass(index)">
                <span class="circle">{{ stepIcons[stepClass(index)] }}</span>
                <span class="step-label">{{step}}</span>
            </li>
        </ol>
        <div class="request-layout">
        <article class="request-card">
            <div class="status-tab">
                <span class="state" :class="request.status">{{ formatLabel(request.status) }}</span>
            </div>
            <p class="kicker">Renewal request #{{ request.id }}</p>
            <h2 class="card-title">{{request.itemName}}</h2>

            <div class="facts">
                <div>
                    <span>Proposed date</span>
                    <strong>{{ formatDate(request.proposedExpirationDate) }}</strong>
                </div>
                <div>
                    <span>Financial review</span>
                    <strong>{{ request.needsFinancialReview ? 'Required' : 'Not required' }}</strong>
                </div>
                <div>
                    <span>Submitted</span>
                    <strong>{{ formatDate(request.submittedAt) }}</strong>
                </div>
            </div>

            <p class="justification">{{ request.justification ?? 'No justification.' }}</p>

            <div class="card-bottom">
                <span class="cost">{{ formatMoney(request.proposedCost) }}</span>
                <RouterLink :to="`/items/${request.itemId}`" class="item-button">Open item</RouterLink>
            </div>
        </article>

        <aside class="side">
            <form v-if="canFinanceReview" @submit.prevent="financeRecommendation" class="action-panel">
                <h3>Financial review</h3>
                <label>
                Recommendation
                    <select v-model="recommendation" required>
                        <option value="" disabled>Select a recommendation for the manager</option>
                        <option>Approve</option>
                        <option>Reject</option>
                    </select>
                 </label>
            <label>
                Comment
                <textarea v-model="comment" rows="4" required></textarea>
            </label>
                <button type="submit" :disabled="financeSaving">{{ financeSaving ? 'Sending...' : 'Send recommendation' }}</button>
            <p v-if="financeError" class="error">{{ financeError }}</p>
            </form>

            <div v-if="canManagerReview || canManagerReject" class="action-panel">
                <h3>Your decision</h3>
                <p v-if="request.status === 'Approved'" class="hint">
                    This approval can no longer be completed, so you can reject it.
                </p>
                <label> Comment (required to reject or to request clarification)
                    <textarea v-model="managerComment" rows="4"></textarea>
                </label>
                <div class="decision-buttons">
                    <button type="button" @click="managerAction('approve')" v-if="canManagerReview" :disabled="managerSaving">Approve</button>
                    <button type="button" @click="managerAction('request-clarification')" v-if="!clarificationUsed && canManagerReview" :disabled="managerSaving">Request Clarification</button>
                    <button type="button" class="danger" @click="managerAction('reject')" v-if="canManagerReject" :disabled="managerSaving">Reject</button>
                </div>
                <p v-if="managerError" class="error">{{ managerError }}</p>
            </div>

            <form @submit.prevent="provideClarification" v-if="canAnswerClarification" class="action-panel">
                <h3>Your answer</h3>
                <p v-if="clarificationQuestion" class="question">"{{ clarificationQuestion }}"</p>
                <label> Answer
                    <textarea v-model="employeeComment" rows="4" required></textarea>
                </label>
                <button type="submit" :disabled="employeeSaving">{{ employeeSaving ? 'Sending...' : 'Send answer' }}</button>
                <p v-if="employeeError" class="error">{{ employeeError }}</p>
            </form>

            <form @submit.prevent="completeRenewal" v-if="canRecordCompletion" class="action-panel">
                <h3>Complete the renewal</h3>
                <p>Do this once the renewal is done. It starts a new period that ends on {{  formatDate(request.proposedExpirationDate) }}.</p>
                <div>
                <button type="submit" :disabled="employeeCompleteSaving">{{ employeeCompleteSaving ? 'Saving...' : 'Complete renewal' }}</button>
                </div>
                <p v-if="employeeCompleteError" class="error">{{ employeeCompleteError }}</p>
            </form>

        </aside>
    </div>

    <h3 class="section-title">History</h3>
    <div class="table-box">
        <table>
            <thead>
                <tr>
                <th>When</th>
                <th>Who</th>
                <th>Step</th>
                <th>Status</th>
                <th>Recommendation</th>
                <th>Comment</th>
                </tr>
            </thead>
            <tbody>
                <tr v-for="(action, index) in request.actions" :key="index">
                    <td>{{formatDateTime(action.createdAt) }}</td>
                    <td>{{ action.actorUserName}}</td>
                    <td>{{ formatLabel(action.type) }}</td>
                    <td><template v-if="action.fromStatus">{{ formatLabel(action.fromStatus)}} -></template> {{ formatLabel(action.toStatus) }}</td>
                    <td>{{ action.recommendation ?? '-' }}</td>
                    <td>{{ action.comment  ?? '-'}}</td>
                </tr>
            </tbody>
        </table>
    </div>

    <h3 class="section-title">Documents</h3>
        <p v-if="documents.length === 0">No documents.</p>
        <div v-for="doc in documents" :key="doc.id" class="file-row">
            <div class="file-info">
                <p class="file-name">{{ doc.originalFileName }}</p>
                <p>{{ Math.ceil(doc.sizeBytes / 1024) }} KB {{ doc.uploadedByUserName }} {{ formatDateTime(doc.uploadedAt) }}</p>
            </div>
            <a :href="`/api/documents/${doc.id}/download`" class="download-link">Download</a>
        </div>
    </div>
</template>
<style scoped>
.stepper {
    display: flex;
    margin: 16px 0 28px;
    padding: 24px 16px;
    list-style: none;
}

.stepper li {
  position: relative;
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  color: #000000;
}

/* the line that comes into this step from the one before */
.stepper li::before {
  content: '';
  position: absolute;
  top: 14px;
  right: 50%;
  width: 100%;
  height: 2px;
  background: #000000;
}

.circle {
  position: relative;
  z-index: 1;
  width: 28px;
  height: 28px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 2px solid #000000;
  border-radius: 50%;
  background: white;
  font-size: 13px;
  font-weight: 700;
  box-sizing: border-box;
}

.stepper li:first-child::before {
    display: none;
}

.stepper li.done .circle    { background: #0a4bff; border-color: #0a4bff; color: white; }
.stepper li.done::before    { background: #0a4bff; }

.stepper li.current .circle { border-color: #0a4bff; color: #0a4bff; }
.stepper li.current::before { background: #0a4bff; }
.stepper li.current         { color: var(--text); font-weight: 500; }

.stepper li.warning .circle { background: #eb9c08; border-color: #eb9c08; color: #111; }
.stepper li.warning::before { background: #eb9c08; }

.stepper li.stopped .circle { background: var(--danger); border-color: var(--danger); color: white; }
.stepper li.stopped::before { background: var(--danger); }

.stepper li.success .circle { background: #0c7c35; border-color: #0c7c35;; color: white; }
.stepper li.success::before { background: #1d4ed8; }

.request-card {
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

.justification {
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

.item-button {
  padding: 14px 28px;
  border-radius: 999px;
  background: rgb(255, 255, 255);
  color: #000000;
  font-weight: 600;
  text-decoration: none;
}

.card-title{
    color: white;
}

.request-layout {
    display: grid;
    grid-template-columns: 3fr 2fr;
    gap: 24px;
    align-items: start;
}

.side {
    display: flex;
    flex-direction: column;
    gap: 16px;
}

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

.action-panel label {
    display: block;
    margin-top: 16px;
    font-weight: 500;
}

.hint {
    margin: 0;
}

.action-panel button {
    margin: 16px 0 0;
    padding: 10px 16px;
}

.decision-buttons {
    display: flex;
    gap: 8px;
}

.section-title {
    margin: 36px 0 12px;
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

.table-box th {
    background: #b5b5b5;
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

@media (max-width: 700px) {
  .facts { flex-direction: column; }
  .facts div + div { padding-left: 0; border-left: none; }
  .step-label { font-size: 0.8em; }
  .request-layout { grid-template-columns: 1fr;}
}
</style>
