<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useNotify } from '@/shared/composables/useNotify'
import { apiRequest } from '@/shared/utils/ApiRequest'
import CollapsibleSection from '@/shared/components/CollapsibleSection.vue'
import { ROUTE_NAMES } from '@/shared/constants/RouteConst'
import { useLoadingPlaceholer } from '@/shared/composables/useLoadingPlaceholder'

const route = useRoute()
const router = useRouter()
const notify = useNotify()
const loadingPlaceholder = useLoadingPlaceholer()

const username = ref<string>('')
const buttonText = computed(() => (loadingPlaceholder.showSkeleton.value ? 'Logining...' : 'Login'))

const login = async function () {
  if (username.value.trim() == '') {
    notify.failure('Username cannot be empty.')
    return
  }

  try {
    loadingPlaceholder.startLoading()

    const result = await apiRequest<{ token: string }>('/api/account/login', {
      method: 'POST',
      body: JSON.stringify({ username: username.value }),
    })

    if (!result.token) {
      throw new Error('Session token not received.')
    }

    localStorage.setItem('token', result.token.toString())

    router.push({ name: ROUTE_NAMES.VOCABULARY })
  } finally {
    loadingPlaceholder.stopLoading()
  }
}

onMounted(() => {
  if (route.query.sessionExpired === 'true') {
    notify.info('Session expired. Please login again')
  }
})
</script>

<template>
  <CollapsibleSection title="Login">
    <form @submit.prevent="login">
      <input class="input" type="text" placeholder="Username..." v-model="username" />
      <button class="big-button" type="submit" :disabled="loadingPlaceholder.isLoading.value">
        {{ buttonText }}
      </button>
    </form>
  </CollapsibleSection>
</template>

<style lang="scss" scoped>
input {
  text-transform: none;
}

button {
  margin-top: 20px;
}
</style>
