export interface MessagePart {
    type: string,
    content: string
}

export type MessageType = 'Normal' | 'Notification' | 'Mention'

export interface ChatMessage {
    platform: string,
    badges: string[],
    displayName: string,
    displayNameColor: string,
    messageParts: MessagePart[],
    messageType?: MessageType
}