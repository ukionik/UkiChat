export interface MessagePart {
    type: string,
    content: string
}

export interface ReplyInfo {
    displayName: string,
    displayNameColor: string,
    messageParts: MessagePart[]
}

export type MessageType = 'Normal' | 'Notification' | 'Mention' | 'Reply'

export interface ChatMessage {
    platform: string,
    badges: string[],
    displayName: string,
    displayNameColor: string,
    messageParts: MessagePart[],
    messageType?: MessageType,
    replyTo?: ReplyInfo
}