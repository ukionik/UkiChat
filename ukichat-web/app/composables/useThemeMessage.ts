import type { ChatMessage } from "~/types/ChatMessage";

// Общее состояние/поведение одного сообщения, нужное всем темам:
// раскрытие удалённого сообщения по клику и проброс клика по ссылке наверх.
export function useThemeMessage(
  props: { message: ChatMessage; allowRevealDeleted: boolean },
  emit: (event: 'linkClick', url: string) => void
) {
  const revealed = ref(false)

  function toggleRevealDeleted() {
    if (!props.allowRevealDeleted || props.message.messageType !== 'Deleted') return
    revealed.value = !revealed.value
  }

  function handleLinkClick(url: string) {
    emit('linkClick', url)
  }

  return { revealed, toggleRevealDeleted, handleLinkClick }
}
