﻿namespace DatabaseVersion.Tasks
{
    using System.Data;

    /// <summary>
    /// An object that executes tasks.
    /// </summary>
    public interface ITaskExecuter
    {
        /// <summary>
        /// Adds a task to be executed.
        /// </summary>
        /// <param name="task">The task to be executed.</param>
        void AddTask(IDatabaseTask task);

        /// <summary>
        /// Executes the tasks.
        /// </summary>
        void ExecuteTasks(IDbConnection connection);
    }
}
