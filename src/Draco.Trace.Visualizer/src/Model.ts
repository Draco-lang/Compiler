export interface MessageModel {
    name: string;
    startTime: number;
    endTime: number;
    children?: MessageModel[];
};

export interface ThreadModel {
    threadId: string;
    rootMessage: MessageModel;
};

export type TraceModel = ThreadModel[];
