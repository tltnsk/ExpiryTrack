import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import LoginView from '@/views/LoginView.vue'
import ItemsView from '@/views/ItemsView.vue'
import ItemDetailView from '@/views/ItemDetailView.vue'
import ItemCreateView from '@/views/ItemCreateView.vue'
import ItemEditView from '@/views/ItemEditView.vue'
import CategoriesView from '@/views/CategoriesView.vue'
import CategoriesCreateView from '@/views/CategoriesCreateView.vue'
import CategoriesEditView from '@/views/CategoriesEditView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', redirect: '/items' },
    { path: '/login', component: LoginView },
    { path: '/items', component: ItemsView },
    { path: '/items/:id', component: ItemDetailView },
    { path: '/items/new', component: ItemCreateView },
    { path: '/items/:id/edit', component: ItemEditView },
    { path: '/categories', component: CategoriesView },
    { path: '/categories/new', component: CategoriesCreateView },
    { path: '/categories/:id/edit', component: CategoriesEditView }
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
