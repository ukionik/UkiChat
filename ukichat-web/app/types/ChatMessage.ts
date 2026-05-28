export interface MessagePart {
    type: string,
    content: string
}

export interface ReplyInfo {
    displayName: string,
    displayNameColor: string,
    messageParts: MessagePart[]
}

export type MessageType = 'Normal' | 'Notification' | 'Mention' | 'Reply' | 'Deleted' | 'ChannelPointsRedemption'

export interface ChatMessage {
    id?: string,
    platform: string,
    badges: string[],
    displayName: string,
    displayNameColor: string,
    messageParts: MessagePart[],
    messageType?: MessageType,
    replyTo?: ReplyInfo,
    rewardTitle?: string,
    rewardCost?: number
}