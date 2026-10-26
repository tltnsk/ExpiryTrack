<script setup lang="ts">
import { ref } from 'vue'

const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')

const errorMessage = ref('')
const successMessage = ref('')
const saving = ref(false)

async function changePassword() {
  errorMessage.value = ''
  successMessage.value = ''

  if (newPassword.value !== confirmPassword.value) {
    errorMessage.value = 'The passwords do not match'
    return
  }

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
      <form @submit.prevent="changePassword">
        <div>
          <label>Current Password <input v-model="currentPassword" type="password" required /></label>
        </div>
        <div>
          <label>New Password <input v-model="newPassword" type="password" required minlength="8"/></label>
        </div>
        <div>
          <label>Confirm New Password <input v-model="confirmPassword" type="password" required minlength="8"/></label>
        </div>
        <button type="submit" :disabled="saving">Change Password</button>
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
        <p v-if="successMessage">{{ successMessage }}</p>
      </form>
</template>

<style scoped>
</style>