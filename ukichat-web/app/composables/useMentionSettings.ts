const mentionNicknames = useState<string[]>('mentionNicknames', () => [])

export function useMentionSettings() {
  return { mentionNicknames }
}
