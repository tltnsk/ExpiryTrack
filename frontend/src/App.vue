<script setup lang="ts">

import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from './stores/auth';
import { useNotificationsStore } from '@/stores/notifications';
import { watch } from 'vue'

const auth = useAuthStore()
const notificationsStore = useNotificationsStore()
const router = useRouter()
const route = useRoute()

async function logout() {
  await auth.logout()
  router.push('/login')
}

watch (() => route.path, () => {
  if (auth.user) notificationsStore.loadUnreadCount()
}, { immediate: true })
</script>

<template>
  <header v-if="auth.user">
    <h1>ExpiryTrack</h1>
    <div v-if="auth.user">
      {{ auth.user.firstName }} {{ auth.user.lastName }} ({{ auth.user.role }})
      <RouterLink to="/change-password">Change Password</RouterLink>
      <button @click="logout">Log out</button>
    </div>
    <nav v-if="auth.user">
      <RouterLink to="/dashboard">Home</RouterLink>
      <RouterLink to="/items">Items</RouterLink>
      <RouterLink to="/categories">Categories</RouterLink>
      <RouterLink v-if="auth.user?.role === 'Administrator'" to="/departments">Departments</RouterLink>
      <RouterLink v-if="auth.user.role === 'Administrator'" to="/users">Users</RouterLink>
      <RouterLink to="/requests">Requests</RouterLink>
      <RouterLink to="/notifications">Notifications {{ notificationsStore.unreadCount }}</RouterLink>
    </nav>
  </header>

  <main>
    <RouterView />
  </main>
</template>

<style scoped>
nav {
  display: flex;
  gap: 20px;
}
</style>
