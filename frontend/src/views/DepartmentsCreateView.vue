<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

const name = ref('')
const isActive = ref(true)


const errorMessage = ref('')
const saving = ref(false)

async function createDepartment() {
    errorMessage.value = ''
    saving.value = true

    const response = await fetch('/api/departments', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            name: name.value,
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
        errorMessage.value = `Could not create the department (error ${response.status}).`
        return
    }
    router.push('/departments')
}

</script>
<template>
<RouterLink to="/departments">Back to departments</RouterLink>
  <h2>New department</h2>

  <form @submit.prevent="createDepartment">
    <div>
      <label>
        Name
        <input v-model="name" required maxlength="100" />
      </label>
    </div>
    <div>
      <label>
        Active
        <input v-model="isActive" type="checkbox" />
      </label>
    </div>

    <button type="submit" :disabled="saving">{{ saving ? 'Saving...' : 'Create department' }}</button>
    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
  </form>
</template>
