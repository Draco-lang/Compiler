export type TraceModel = ThreadModel[];

export interface ThreadModel {
    threadId: string;
    rootMessage: MessageModel;
};

export interface MessageModel {
    name: string;
    startTime: number;
    endTime: number;
    children?: MessageModel[];
};
