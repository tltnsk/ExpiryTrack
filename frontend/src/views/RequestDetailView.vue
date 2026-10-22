<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { formatDate, formatDateTime} from '@/utils/format'

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
const canManagerReject = computed(() => auth.user?.role === 'DepartmentManager' && (request.value?.status === 'PendingManagerApproval' || request.value?.status === 'ClarificationRequired'))
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

async function loadRequest() {
    const response = await fetch(`/api/requests/${id}`)
    if (response.ok) {
        const data: RenewalRequest = await response.json()
        request.value = data
    } else if (response.status === 404 || response.status === 400) {
        errorMessage.value = 'Item not found.'
    } else {
        errorMessage.value = `Could not load item (error ${response.status}).`
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

</script>

<template>
    <RouterLink to="/requests">Back to Renewal Requests</RouterLink>

    <p v-if="loading">Loading...</p>

    <p v-else-if="errorMessage"> {{  errorMessage }}</p>
    <div v-else-if="request">
        <h3>Details</h3>
        <dl>
            <dt>Status</dt>
            <dd>{{ request.status}}</dd>
            <dt>Proposed Expiration Date</dt>
            <dd>{{ formatDate(request.proposedExpirationDate )}}</dd>
            <dt>Proposed Cost</dt>
            <dd>{{ request.proposedCost ?? '-' }}</dd>
            <dt>Justification</dt>
            <dd>{{ request.justification ?? '-' }}</dd>
            <dt>Submitted At</dt>
            <dd>{{ formatDateTime(request.submittedAt) }}</dd>
            <dt>Decided At</dt>
            <dd>{{ formatDateTime(request.decidedAt) ?? '-' }}</dd>
            <dt>Completed At</dt>
            <dd>{{ formatDateTime(request.completedAt) ?? '-' }}</dd>
        </dl>

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
                <td>{{action.createdAt }}</td>
                <td>{{ action.actorUserName}}</td>
                <td>{{ action.type }}</td>
                <td>{{ action.fromStatus ?? '-' }} -> {{ action.toStatus }}</td>
                <td>{{ action.recommendation ?? '-' }}</td>
                <td>{{ action.comment  ?? '-'}}</td>
            </tr>
        </tbody>
    </table>

    <form v-if="canFinanceReview" @submit.prevent="financeRecommendation">
    <div>
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
    <p v-if="financeError">{{ financeError }}</p>
    </form>

    <div v-if="canManagerReview || canManagerReject">
        <div>
            <label> Comment (Required for Reject and Request)
            <textarea v-model="managerComment"></textarea>
            </label>
        </div>
        <button type="button" @click="managerAction('approve')" v-if="canManagerReview" :disabled="managerSaving">Approve</button>
        <button type="button" @click="managerAction('reject')" v-if="canManagerReject" :disabled="managerSaving">Reject</button>
        <button type="button" @click="managerAction('request-clarification')" v-if="!clarificationUsed && canManagerReview" :disabled="managerSaving">Request Clarification</button>
        <p v-if="managerError">{{ managerError }}</p>
    </div>
      <form @submit.prevent="provideClarification" v-if="canAnswerClarification">
        <h3>Your answer</h3>
        <div>
            <label> Provide Clarification
            <textarea v-model="employeeComment" required></textarea>
            </label>
        </div>
        <div>
          <button type="submit" :disabled="employeeSaving">Send clarification</button>
        </div>
        <p v-if="employeeError">{{ employeeError }}</p>
    </form>

    <form @submit.prevent="completeRenewal" v-if="canRecordCompletion">
        <div>
          <button type="submit" :disabled="employeeCompleteSaving">Complete</button>
        </div>
        <p v-if="employeeCompleteError">{{ employeeCompleteError }}</p>
    </form>
    </div>
</template>