<script setup lang="ts">
import MenuTabSettingsItem from '~/components/MenuTabSettingsItem.vue'

import type { DonationAlertsAuthStatus } from '~/types/DonationAlertsAuth'

interface Props {
  donationAlertsAuth: DonationAlertsAuthStatus
}

const props = defineProps<Props>()
const emit = defineEmits<{
  'authorize-donation-alerts': []
  'logout-donation-alerts': []
}>()

const { t } = useI18n()

const activeSub = ref('donationAlerts')
</script>

<template>
  <div>
    <div class="flex gap-1 px-4 pt-4 border-b border-gray-800">
      <MenuTabSettingsItem
        :title="t('settings.donationAlerts.name')"
        :active="activeSub === 'donationAlerts'"
        icon="/images/donation-alerts.svg"
        @click="activeSub = 'donationAlerts'"
      />
    </div>
    <div class="p-6 space-y-4 max-w-xl">
      <template v-if="activeSub === 'donationAlerts'">
        <div class="space-y-3">
          <div>
            <p class="text-sm font-medium text-gray-200">{{ t('settings.donationAlerts.authTitle') }}</p>
            <p class="text-xs text-gray-500 mt-1">{{ t('settings.donationAlerts.authHint') }}</p>
          </div>
          <div class="flex items-center gap-3">
            <template v-if="props.donationAlertsAuth.authorized">
              <span class="text-sm text-green-400">
                {{ t('settings.donationAlerts.authorizedAs', { name: props.donationAlertsAuth.name }) }}
              </span>
              <UButton color="error" variant="soft" size="sm" @click="emit('logout-donation-alerts')">
                {{ t('settings.donationAlerts.logout') }}
              </UButton>
            </template>
            <template v-else>
              <span class="text-sm text-gray-500">{{ t('settings.donationAlerts.notAuthorized') }}</span>
              <UButton color="primary" variant="solid" size="sm"
                       @click="emit('authorize-donation-alerts')">
                {{ t('settings.donationAlerts.authorize') }}
              </UButton>
            </template>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>
