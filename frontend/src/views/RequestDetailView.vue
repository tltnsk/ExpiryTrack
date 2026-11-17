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

onMounted(loadRequest)
onMounted(async () => {
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

const percent = computed(() => 
    steps.value.length === 0 ? 0 : Math.round(doneSteps.value / steps.value.length * 100))

</script>

<template>
    <RouterLink to="/requests" class="back-link">Back to Renewal Requests</RouterLink>

    <p v-if="loading">Loading...</p>

    <p v-else-if="errorMessage" class="error"> {{  errorMessage }}</p>
    <div v-else-if="request">
        <h2>Renewal Request for <RouterLink :to="`/items/${request.itemId}`">{{ request.itemName }}</RouterLink></h2>
        <div class="request-layout">
            <div class="card">
                <dl>
                    <dt>Status</dt>
                    <dd><span class="state" :class="request.status">{{ formatLabel(request.status) }}</span></dd>
                    <dt>Proposed Expiration Date</dt>
                    <dd>{{ formatDate(request.proposedExpirationDate )}}</dd>
                    <dt>Proposed Cost</dt>
                    <dd>{{ formatMoney(request.proposedCost)  }}</dd>
                    <dt>Justification</dt>
                    <dd>{{ request.justification ?? '-' }}</dd>
                    <dt>Submitted At</dt>
                    <dd>{{ formatDateTime(request.submittedAt) }}</dd>
                    <dt>Decided At</dt>
                    <dd>{{ formatDateTime(request.decidedAt)}}</dd>
                    <dt>Completed At</dt>
                    <dd>{{ formatDateTime(request.completedAt) }}</dd>
                </dl>
            </div>  
            <aside class="progress-box">
                <div class="progress-header">
                    <h3>Progress</h3>
                    <div class="progress-bar">
                        <div class="progress-fill" :class="{ rejected: isRejected}" :style="{ width: percent + '%'}"></div>
                    </div>
                    <span>{{ doneSteps }}/{{ steps.length }}</span>
                </div>
                <div class="progress-inner">
                    <ol class="steps">
                        <li v-for="(step, index) in steps" :key="step" :class="{done: index < doneSteps,
                            current: index === doneSteps && !isRejected,
                            stopped: index === doneSteps && isRejected
                        }">{{ step }}</li>
                    </ol>
                </div>
            </aside>                
        </div>

    <h3>History</h3>
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

    <form v-if="canFinanceReview" @submit.prevent="financeRecommendation" class="card section">
    <div>
        <h3>Financial review</h3>
        <label>
        Recommendation
        <select v-model="recommendation" required>
          <option value="" disabled>Select a recommendation for the manager</option>
          <option>Approve</option>
          <option>Reject</option>
        </select>
      </label>
    </div>
    <div>
      <label>
        Comment
        <textarea v-model="comment" required></textarea>
      </label>
    </div>
    <div>
          <button type="submit" :disabled="financeSaving">Submit recommendation</button>
    </div>
    <p v-if="financeError" class="error">{{ financeError }}</p>
    </form>

    <div v-if="canManagerReview || canManagerReject" class="card section">
        <h3>Your decision</h3>
        <p v-if="request.status === 'Approved'">
            This approval can no longer be completed, so you can reject it. 
        </p>
        <label> Comment (required to reject or to request clarification)
            <textarea v-model="managerComment" id="managerComment"></textarea>
        </label>
        <div id="managerButtons">
            <button type="button" @click="managerAction('approve')" v-if="canManagerReview" :disabled="managerSaving">Approve</button>
            <button type="button" @click="managerAction('request-clarification')" v-if="!clarificationUsed && canManagerReview" :disabled="managerSaving">Request Clarification</button>
            <button type="button" class="danger" @click="managerAction('reject')" v-if="canManagerReject" :disabled="managerSaving">Reject</button>
        </div>
        <p v-if="managerError" class="error">{{ managerError }}</p>
    </div>

      <form @submit.prevent="provideClarification" v-if="canAnswerClarification" class="card section">
        <h3>Your answer</h3>
        <div>
            <label> Provide Clarification
            <textarea v-model="employeeComment" required></textarea>
            </label>
        </div>
        <div>
          <button type="submit" :disabled="employeeSaving">Send clarification</button>
        </div>
        <p v-if="employeeError" class="error">{{ employeeError }}</p>
    </form>

    <form @submit.prevent="completeRenewal" v-if="canRecordCompletion" class="card section">
        <h3>Complete the renewal</h3>
        <p>Do this once the renewal is done. It starts a new period that ends on {{  formatDate(request.proposedExpirationDate) }}.</p>
        <div>
          <button type="submit" :disabled="employeeCompleteSaving">Complete</button>
        </div>
        <p v-if="employeeCompleteError" class="error">{{ employeeCompleteError }}</p>
    </form>

    <h3>Documents</h3>
        <p v-if="documents.length === 0">No documents.</p>
        <table v-else>
        <thead>
            <tr>
              <th>File Name</th>
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

.error {
    color: var(--danger);
}

.section {
    max-width: 640px;
    margin: 24xp 0;
    padding: 20px 24px;
}

#managerButtons {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    margin-top: 12px;
}

.request-layout {
    display: grid;
    grid-template-columns: 2fr 1fr;
    gap: 24px;
    align-items: start;
}

.progress-box {
    background: #f4f4f5;
    border-radius: 20px;
    padding: 6px;
}

.progress-header {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 10px 14px;
}

.progress-header h3 {
    margin: 0;
    flex: 1;            
}

.progress-bar {
    width: 90px;
    height: 8px;
    background: white;
    border-radius: 999px;
    overflow: hidden;
}

.progress-fill {
    height: 100%;
    background: var(--light-blue);
    border-radius: 999px;
    transition: width 0.3s;
}

.progress-fill.rejected {
    background: var(--danger);
}

.progress-inner {
    background: white;
    border-radius: 16px;
    padding: 14px 18px;
    box-shadow: 0 1px 4px rgba(0, 0, 0, 0.06);
}

.steps {
    list-style: none;
    padding: 0;
    margin: 0;
}

.steps li {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 8px 0;
    color: #b0b0b0;              
}

.steps li::before {
    content: '';
    width: 18px;
    height: 18px;
    flex-shrink: 0;
    border-radius: 50%;
    border: 2px dashed #c4c4c4;   
    display: grid;
    place-items: center;
    font-size: 11px;
    font-weight: 700;
    color: white;
}

.steps li.done { color: #4b5563; }
.steps li.done::before { content: '✓'; background: #43c825; border: 2px solid #1e8644; }

.steps li.current { color: var(--text); font-weight: 500; }
.steps li.current::before { border-color: #6b7280; }

.steps li.stopped { color: var(--danger); font-weight: 500; }
.steps li.stopped::before { content: '✕'; background: var(--danger); border: 2px solid var(--danger); }

.card {
    background: #f4f4f5;
}

@media (max-width: 900px) {
    .request-layout {grid-template-columns: 1fr;}
}

</style>
