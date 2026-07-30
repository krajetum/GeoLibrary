import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      component: () => import('@/pages/home.vue'),
    },
    {
      path: '/app/',
      component: () => import('@/layouts/admin.vue'),
      meta: { requiresAuth: true },
      children: [
        {
          path: '',
          component: () => import('@/pages/app/dashboard.vue'),
        },
        {
          path: 'libraries',
          name: 'libraries',
          children: [
            {
              path: '',
              component: () => import('@/pages/app/libraries/list.vue'),
            },
            {
              path: ':id',
              name: 'library-detail',
              children: [
                {
                  path: '',
                  component: () => import('@/pages/app/libraries/detail.vue'),
                },
                {
                  path: 'edit',
                  // meta.title è l'ultima briciola delle pagine di azione (vedi breadcrumbs.vue)
                  meta: { title: 'Modifica' },
                  component: () => import('@/pages/app/libraries/edit.vue'),
                },
                {
                  path: 'book',
                  children: [
                    {
                      path: 'new',
                      meta: { title: 'Nuovo libro' },
                      component: () => import('@/pages/app/libraries/book/new.vue'),
                    },
                    {
                      path: 'import',
                      meta: { title: 'Importa libri' },
                      component: () => import('@/pages/app/libraries/book/import.vue'),
                    },
                    {
                      path: ':bookId',
                      component: () => import('@/pages/app/libraries/book/detail.vue'),
                    },
                    {
                      path: ':bookId/edit',
                      meta: { title: 'Modifica' },
                      component: () => import('@/pages/app/libraries/book/edit.vue'),
                    },
                  ],
                },
              ],
            },
            {
              path: 'new',
              meta: { title: 'Nuova libreria' },
              component: () => import('@/pages/app/libraries/new.vue'),
            },
          ],
        },
      ],
    },
  ],
})

router.beforeEach((to) => {
  const auth = useAuthStore()
  // requiresAuth su un record padre protegge tutta la sua sottostruttura:
  // matched contiene l'intera catena (padre + figli) della route di destinazione.
  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth)
  if (requiresAuth && !auth.isAuthenticated) {
    auth.login(to.fullPath)
    return false
  }
})

export default router
