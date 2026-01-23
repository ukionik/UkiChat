export interface MessagePart {
    type: string,
    content: string
}

export interface ChatMessage {
    platform: string,
    badges: string[],
    displayName: string,
    messageParts: MessagePart[]
}