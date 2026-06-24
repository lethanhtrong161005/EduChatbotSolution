window.ChatEnums = Object.freeze({

    MessageStatus: Object.freeze({
        Pending: 0,
        Generating: 1,
        Streaming: 101,
        Completed: 2,
        Failed: 3,
    }),

    ChatRole: Object.freeze({
        System: 0,
        User: 1,
        Assistant: 2,
    }),
});
