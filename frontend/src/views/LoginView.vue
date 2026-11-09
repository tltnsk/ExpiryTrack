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

  if (response.status === 401) {
    errorMessage.value = 'Invalid email or password.'
    return
  }

  if (!response.ok) {
    errorMessage.value = `Something went wrong (error ${response.status}).`
    return 
  }

  auth.user = await response.json()

  // redirect user to dashboard 
  router.push('/dashboard')
}

</script>

<template>
  <div class="login-page">
    <div class="login-left">
        <h1>Track your company items, expiration dates, renewal requests and renewal workflow all in one place.</h1>
    </div>
    <div class="login-right">
      <form class="login-card" @submit.prevent="login">
        <h1>ExpiryTrack</h1>
        <label>Email <input v-model="email" type="email" required /></label>
        <label>Password <input v-model="password" type="password" required /></label>
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
          <button type="submit" :disabled="loggingIn"> {{ loggingIn ? 'Logging in...' : 'Log in' }}</button>
        </form>
    </div>
    </div>
</template>

<style scoped>
.login-page {
  display: flex;
  flex-direction: row;
  background: white;
  position: fixed;
  inset: 0;
}

.login-left {
  width: 33.33%;
  display: flex;
  justify-content: center;
  align-items: flex-end;
  padding: 40px;
  box-sizing: border-box;
  background-color: var(--sidebar);
}

.login-right {
  width: 66.67%;
  display: flex;
  justify-content: center;
  align-items: center;
}

.login-left h1 {
  font-size: 1.5rem;
  text-align: left;
  line-height: 1.3;
}

.login-card {
  width: 100%;
  max-width: 340px;
  margin: 0;
  padding: 32px;
  background: white;
  border-radius: 15px;
  box-sizing: border-box;
}

.login-card h1 {
  font-size: 1.8rem;
  text-align: center;
  margin-bottom: 24px;
}

label {
  display: block;
  margin-bottom: 16px;
  font-size: 0.9em;
  font-weight: 500;
  color: var(--primary);
}

.error {
  margin: 0 0 12px;
  font-size: 0.9em;
}

@media (max-width: 700px) {
  .login-left {display: none;}
  .login-right {width: 100%;}
}
</style>
