namespace InFlux
{
    /// <summary>
    /// Allows you to add code to the queue for execution.
    /// Execution is considered automatic, and in sequence.
    /// </summary>
    public static class QueuedActions
    {
        private static Queue<Action> queue = new();

        private static bool busy = false;

        /// <summary>
        /// Add an action to the current queue.  Will execute if first item in queue,
        /// therefore to avoid possible unwanted side-effects of adding multiple Actions to the queue,
        /// use the <see cref="AddRange(Action[])"/> method instead.
        /// </summary>
        [DebuggerStepThrough]
        public static void Add(Action code)
        {
            queue.Enqueue(code);

            if (!busy)
                processQueue();
        }

        /// <summary>
        /// Adds any number of Actions to be executes to the queue, and then processes the queue,
        /// if the queue is not already being processed.
        /// </summary>
        [DebuggerStepThrough]
        public static void AddRange(params Action[] codeCollection)
        {
            foreach (var codeItem in codeCollection)
                queue.Enqueue(codeItem);

            processQueue();
        }

        /// <summary>
        /// Adds any number of Actions to be executes to the queue, and then processes the queue,
        /// if the queue is not already being processed.
        /// </summary>
        [DebuggerStepThrough]
        public static void AddRange(IEnumerable<Action> codeCollection)
        {
            foreach (var codeItem in codeCollection)
                queue.Enqueue(codeItem);

            processQueue();
        }

        [DebuggerStepThrough]
        private static void processQueue()
        {
            if (busy) return;

            busy = true;

            // given that 'action' can call the Add or AddRange methods, the queue may have grown.
            while (queue.TryDequeue(out var action))
            {
                action();
            }

            busy = false;
        }
    }
}