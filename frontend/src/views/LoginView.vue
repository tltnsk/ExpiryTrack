<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()

const email = ref('')
const password = ref('')
const errorMessage = ref('')
const loggingIn = ref(false)

async function login() {
  errorMessage.value = ''
  loggingIn.value = true

  const response = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json'},
    body: JSON.stringify({email: email.value, password: password.value}),
  })

  loggingIn.value = false

  if (response.status == 401) {
    errorMessage.value = 'Invalid email or password.'
    return
  }

  if (!response.ok) {
    errorMessage.value = `Something went wrong (error ${response.status}).`
    return 
  }

  auth.user = await response.json()

  // redirect user to items 
  router.push('/dashboard')
}

</script>

<template>
  <div class="login-page">
    <h1>ExpiryTrack</h1>
    <form class="login-card" @submit.prevent="login">

      <p class="subtitle">Log in to your company account.</p>
      <label>Email <input v-model="email" type="email" required /></label>
      <label>Password <input v-model="password" type="password" required /></label>
      <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
        <button type="submit" :disabled="loggingIn"> {{ loggingIn ? 'Logging in...' : 'Log in' }}</button>
      </form>
    </div>
</template>

<style scoped>
.login-page {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  min-height: calc(100vh - 48px);
  background: white;
  position: fixed;
  inset: 0;
}

.login-card {
  width: 100%;
  max-width: 340px;
  margin: 0;
  padding: 32px;
  background: white;
  border: 0.5px solid var(--border);
  border-radius: 15px;
}

h1 {
  display: flex;
  justify-content: center;
  margin: 0 auto;
  font-size: 1.6em;
  padding: 0 auto;
}

.subtitle {
  margin: 0 0 24px;
  color: var(--text);
  font-size: 0.9em;
}

h1 {
  margin: 0 0 24px;
  font-size: 1.8em;
  text-align: center;
}


label {
  display: block;
  margin-bottom: 16px;
  font-size: 0.9em;
  font-weight: 500;
  color: var(--navy);
}


.error {
  margin: 0 0 12px;
  font-size: 0.9em;
}
</style>
