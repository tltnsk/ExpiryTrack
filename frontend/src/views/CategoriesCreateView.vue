<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const name = ref('')
const description = ref('')
const warningPeriodDays = ref('')
const requiresFinancialReview = ref(false)
const financeReviewThreshold = ref('')
const isActive = ref(true)

const errorMessage = ref('')
const saving = ref(false)
const errors = ref<Record<string,string>>({})

function onInvalid(event: Event) {
  const input = event.target as HTMLInputElement
  event.preventDefault() 
  errors.value[input.name] = input.validity.valueMissing
      ? 'This field is required.'
      : input.validationMessage
}

async function createCategory() {
    errorMessage.value = ''
    saving.value = true

    const response = await fetch('/api/categories', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            name: name.value,
            description: description.value || null,
            warningPeriodDays: Number(warningPeriodDays.value),
            requiresFinancialReview: requiresFinancialReview.value,
            financeReviewThreshold: financeReviewThreshold.value === '' ? null : Number(financeReviewThreshold.value),
            isActive: isActive.value
        }),
    })

    saving.value = false
    if (response.status === 400) {
        const contentType = response.headers.get('Content-Type') ?? ''
        if (contentType.includes('json')) {
            const problem = await response.json()
            errorMessage.value = Object.values(problem.errors).flat().join(' ')
        } else {
            errorMessage.value = await response.text()
        }
        return
    }
    if (response.status === 409) {
        errorMessage.value = await response.text()
        return
    }
    if (!response.ok) {
        errorMessage.value = `Could not create the category (error ${response.status}).`
        return
    }
    router.push('/categories')
}

</script>
<template>
  <h2>New category</h2>

  <form @submit.prevent="createCategory" @invalid.capture="onInvalid" @input="errors = {}">
    <div>
      <label>
        Name <span class="required">*</span>
        <input v-model="name" name="name" required maxlength="100" />
      </label>
      <p v-if="errors.name" class="field-error">{{ errors.name }}</p>
    </div>
    <div>
      <label>
        Description
        <textarea v-model="description" rows="3" maxlength="255"></textarea>
      </label>
    </div>
    <div>
      <label>
        Warning Period (days) <span class="required">*</span>
        <input v-model="warningPeriodDays" name="warningPeriodDays" type="number" min="1" max="365" required/>
      </label>
      <p v-if="errors.warningPeriodDays" class="field-error">{{ errors.warningPeriodDays }}</p>
    </div>
    <div>
      <label>
        Requires Financial Review
        <input type="checkbox" v-model="requiresFinancialReview" />
      </label>
    </div>
    <div v-if="requiresFinancialReview">
      <label>
        Finance Review Threshold
        <input v-model="financeReviewThreshold" name="financeReviewThreshold" type="number" min="0" step="0.01"/>
      </label>
      <p v-if="errors.financeReviewThreshold" class="field-error">{{ errors.financeReviewThreshold }}</p>
    </div>
    <div>
      <label>
        Active
        <input v-model="isActive" type="checkbox" />
      </label>
    </div>

    <button type="submit" :disabled="saving">{{ saving ? 'Saving...' : 'Create category' }}</button>
    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
  </form>
</template>
