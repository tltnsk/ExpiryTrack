<script setup lang="ts">
import { ref } from 'vue'

const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')

const errorMessage = ref('')
const successMessage = ref('')
const saving = ref(false)

const errors = ref<Record<string,string>>({})

function onInvalid(event: Event) {
  const input = event.target as HTMLInputElement
  event.preventDefault() 
    // a new attempt, so an old message from the server no longer applies
  errorMessage.value = ''
  successMessage.value = ''
  errors.value[input.name] = input.validity.valueMissing
      ? 'This field is required.'
      : input.validity.tooShort 
          ? 'The password must be at least 8 characters.'
          : input.validationMessage
}

async function changePassword() {
  errorMessage.value = ''
  successMessage.value = ''

  if (newPassword.value !== confirmPassword.value) {
    errors.value.confirmPassword = 'The passwords do not match'
    return
  }

  saving.value = true
  const response = await fetch('/api/auth/change-password', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json'},
    body: JSON.stringify({currentPassword: currentPassword.value, newPassword: newPassword.value}),
  })

  saving.value = false

    if (response.status === 400) {
    const contentType = response.headers.get('Content-Type') ?? ''
    if (contentType.includes('json')) {
      // validation errors 
      const problem = await response.json()
      errorMessage.value = Object.values(problem.errors).flat().join(' ')
    } else {
      // plain text s
      errorMessage.value = await response.text()
    }
    return
  }
  if (!response.ok) {
    errorMessage.value = `Could not change the password (error ${response.status}).`
    return
  }

  successMessage.value = 'Your password has been changed.'
  currentPassword.value = ''
  newPassword.value = ''
  confirmPassword.value = ''
}

</script>

<template>
    <h2>Change Password</h2>
      <form @submit.prevent="changePassword" @invalid.capture="onInvalid" @change="errors = {}">
        <div>
          <label>Current Password <span class="required">*</span> <input v-model="currentPassword" name="currentPassword" type="password" required /></label>
          <p v-if="errors.currentPassword" class="field-error">{{ errors.name }}</p>
        </div>
        <div>
          <label>New Password <span class="required">*</span> <input v-model="newPassword" name="newPassword"  type="password" required minlength="8"/></label>
          <p v-if="errors.newPassword" class="field-error">{{ errors.newPassword }}</p>
        </div>
        <div>
          <label>Confirm New Password <span class="required">*</span> <input v-model="confirmPassword" name="confirmPassword"  type="password" required/></label>
          <p v-if="errors.confirmPassword" class="field-error">{{ errors.confirmPassword }}</p>
        </div>
        <button type="submit" :disabled="saving">Change Password</button>
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
        <p v-if="successMessage">{{ successMessage }}</p>
      </form>
</template>

<style scoped>
</style>4