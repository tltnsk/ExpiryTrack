import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import LoginView from '@/views/LoginView.vue'
import ItemsView from '@/views/ItemsView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', redirect: '/items' },
    { path: '/login', component: LoginView },
    { path: '/items', component: ItemsView }
  ],
})

// runs before each navigation
router.beforeEach(async (to) => {
  const auth = useAuthStore()

  // if user is not authenticated, redirect them to login page
  if (auth.user === null) {
    await auth.loadMe()
  }

  if (auth.user === null && to.path !== '/login') {
    return '/login'
  }

  // if authenticated and try to access login page, redirect them to items 
  if (auth.user !== null && to.path === '/login') {
    return '/items'
  }
})

export default router
