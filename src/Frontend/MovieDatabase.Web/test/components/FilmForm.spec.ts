import { fireEvent, render, screen } from '@testing-library/vue'
import { describe, expect, it } from 'vitest'
import FilmForm from '../../app/components/FilmForm.vue'

// Stands in for the real `ImageUploader`, which does its own `$fetch('/api/upload', ...)`
// call -- irrelevant to testing FilmForm's own wiring of the `uploaded` event.
const fakeUploader = {
  template: '<button type="button" @click="$emit(\'uploaded\', { id: \'blob-1\', fileName: \'cover.png\', url: \'https://cdn.test/images/blob-1.png\' })">Upload</button>',
}

function renderForm(props: Record<string, unknown> = {}) {
  return render(FilmForm, {
    props,
    global: { stubs: { ImageUploader: fakeUploader } },
  })
}

describe('FilmForm cover image', () => {
  it('shows the placeholder when nothing has been uploaded and there is no current thumbnail', () => {
    renderForm()

    expect(screen.getByText('No image selected')).toBeTruthy()
    expect(screen.queryByRole('img')).toBeNull()
  })

  it('shows the current thumbnail when one is given', () => {
    renderForm({ currentThumbnailUrl: 'https://cdn.test/content/film-1.jpg' })

    expect(screen.getByRole('img').getAttribute('src')).toBe('https://cdn.test/content/film-1.jpg')
  })

  it('shows the newly uploaded preview instead of the current thumbnail once an image is uploaded', async () => {
    renderForm({ currentThumbnailUrl: 'https://cdn.test/content/film-1.jpg' })

    await fireEvent.click(screen.getByText('Upload'))

    expect(screen.getByRole('img').getAttribute('src')).toBe('https://cdn.test/images/blob-1.png')
  })

  it('includes thumbnailId in the emitted submit value after an upload', async () => {
    const { emitted } = renderForm()

    await fireEvent.click(screen.getByText('Upload'))
    await fireEvent.click(screen.getByText('Save'))

    const submitted = emitted().submit?.[0]?.[0] as { thumbnailId?: string }
    expect(submitted.thumbnailId).toBe('blob-1')
  })

  it('omits thumbnailId from the emitted submit value when nothing was uploaded', async () => {
    const { emitted } = renderForm()

    await fireEvent.click(screen.getByText('Save'))

    const submitted = emitted().submit?.[0]?.[0] as { thumbnailId?: string }
    expect(submitted.thumbnailId).toBeUndefined()
  })
})
