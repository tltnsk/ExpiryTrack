<script setup lang="ts">

import { useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth';

const auth = useAuthStore()
const router = useRouter()


async function logout() {
  await auth.logout()
  router.push('/login')
}
</script>

<template>
  <header>
    <h1>ExpiryTrack</h1>
    <div v-if="auth.user">
      {{ auth.user.firstName }} {{ auth.user.lastName }} ({{ auth.user.role }})
      <button @click="logout">Log out</button>
    </div>
    <nav v-if="auth.user">
      <RouterLink to="/items">Items</RouterLink>
      <RouterLink to="/categories">Categories</RouterLink>
      <RouterLink v-if="auth.user?.role === 'Administrator'" to="/departments">Departments</RouterLink>
      <RouterLink v-if="auth.user.role === 'Administrator'" to="/users">Users</RouterLink>
      <RouterLink to="/requests">Requests</RouterLink>
    </nav>
  </header>

  <main>
    <RouterView />
  </main>
</template>

<style scoped></style>
