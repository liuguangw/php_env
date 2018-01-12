namespace php_env.items
{
    /// <summary>
    /// 任务结果封装
    /// </summary>
    public class TaskResult
    {
        /// <summary>
        /// 任务是否完成
        /// </summary>
        public bool success { get; }
        /// <summary>
        /// 任务失败原因
        /// </summary>
        public string message { get; }

        /// <summary>
        /// 任务执行结果
        /// </summary>
        /// <param name="success"></param>
        /// <param name="message"></param>
        public TaskResult(bool success, string message)
        {
            this.success = success;
            this.message = message;
        }

        /// <summary>
        /// 任务执行成功
        /// </summary>
        public TaskResult() : this(true, "")
        {
        }

        /// <summary>
        /// 任务执行失败
        /// </summary>
        /// <param name="message">错误消息</param>
        public TaskResult(string message) : this(false, message)
        {
        }

        /// <summary>
        /// 任务执行失败
        /// </summary>
        /// <param name="e">异常</param>
        public TaskResult(System.Exception e) : this(e.Message)
        {
        }
    }
}
