<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()

const email = ref('')
const password = ref('')
const errorMessage = ref('')

async function login() {
  errorMessage.value = ''

  const response = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json'},
    body: JSON.stringify({email: email.value, password: password.value}),
  })

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
  router.push('/items')
}

</script>

<template>
  <div class="login-page">
    <div class="login-card">
      <h1>ExpiryTrack</h1>
      <p class="subtitle">Log in to your company account.</p>
      <form @submit.prevent="login">
        <div>
          <label>Email <input v-model="email" type="email" required /></label>
        </div>
        <div>
          <label>Password <input v-model="password" type="password" required /></label>
        </div>
        <button type="submit">Log in</button>
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
      </form>
    </div>
  </div>
</template>

<style scoped>
.login-page {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 80vh;
}

.login-card {
  width: 100%;
  max-width: 340px;
  padding: 32px;
  background: white;
  border: 0.5px solid var(--border);
  border-radius: 15px;
}

.subtitle {
  text-align: center;
  color: var(--navy);
  font-weight: 600;
  font-size: 18px;
}

h1 {
  margin: 0;
  text-align: center;
}

form > div {
  margin-bottom: 16px;
}

label {
  display: block;
  font-size: 0.9em;
  font-weight: 500;
  color: var(--navy);
}

input {
  display: block;
  box-sizing: border-box;
  width: 100%;
  margin-top: 6px;
  padding: 8px 8px;
  border: 1px solid var(--border);
  border-radius: 10px;
  font: inherit;
  color: var(--text);
}

input:focus {
  border-color: var(--navy);
}

button {
  width: 100%;
  margin-top: 8px;
  padding: 10px;
  background: var(--navy);
  color: white;
  border: none;
  border-radius: 10px;
  font-weight: 500;
  cursor: pointer;
}

button:hover {
  opacity: 0.85;
}

.error {
  margin: 10px 0 0;
  text-align: center;
}
</style>
