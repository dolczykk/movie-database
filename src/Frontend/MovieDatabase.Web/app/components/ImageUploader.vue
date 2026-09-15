<script setup lang="ts">
import { describeUploadClientError } from '../../lib/uploadError'
import { validateUpload } from '../../lib/uploadValidation'

// Not mounted on any page yet: `CreateFilmInput`/`EditFilmInput` have no
// thumbnail/blob field (the API's write path for `Film.Thumbnail` was
// removed before this plan began), so there is nothing to attach an
// uploaded blob to. Built and tested standalone so that wiring it up, once
// that write path exists, is a small change rather than a new feature.
const emit = defineEmits<{ uploaded: [blob: { id: string, fileName: string, url: string }] }>()

const error = ref<string | null>(null)
const pending = ref(false)

async function onChange(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) {
    return
  }

  error.value = validateUpload(file)
  if (error.value) {
    input.value = ''
    return
  }

  pending.value = true
  try {
    const form = new FormData()
    form.append('file', file)
    const blob = await $fetch<{ id: string, fileName: string, url: string }>('/api/upload', {
      method: 'POST',
      body: form,
    })
    emit('uploaded', blob)
  }
  catch (fetchError) {
    // `describeUploadClientError` reads the `statusMessage` the server
    // route crafted (see `server/api/upload.post.ts` / `lib/uploadError.ts`)
    // -- including the preflight/CSRF diagnostic case -- rather than
    // showing a hardcoded generic string here.
    error.value = describeUploadClientError(fetchError)
  }
  finally {
    pending.value = false
    input.value = ''
  }
}
</script>

<template>
  <div>
    <input
      type="file"
      accept="image/png,image/jpeg,image/jpg,image/webp"
      :disabled="pending"
      @change="onChange"
    >
    <p v-if="pending" class="mt-1 text-sm text-gray-500 dark:text-gray-400">
      Uploading…
    </p>
    <p v-if="error" class="mt-1 text-sm text-red-600 dark:text-red-400">
      {{ error }}
    </p>
  </div>
</template>
