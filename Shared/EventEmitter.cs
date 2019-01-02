using System.Collections.Generic;
using System.Threading.Tasks;

namespace TreeRouter.Shared
{
    public delegate Task Del();
    public delegate Task Del<T1>(T1 arg1);
    public delegate Task Del<T1, T2>(T1 arg1, T2 arg2);
    public delegate Task Del<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3);
	
    public class EventEmitter : List<Del>
    {
        public Task Invoke()
        {
            var tasks = new List<Task>();
            foreach (var del in this)
            {
                if (del.Invoke() is Task task)
                    tasks.Add(task);
            }
            return tasks.Count == 0 ? Task.CompletedTask : Task.WhenAll(tasks);
        }
    }
	
    public class EventEmitter<T1> : List<Del<T1>>
    {
        public Task Invoke(T1 arg1)
        {
            var tasks = new List<Task>();
            foreach (var del in this)
            {
                if (del.Invoke(arg1) is Task task)
                    tasks.Add(task);
            }
            return tasks.Count == 0 ? Task.CompletedTask : Task.WhenAll(tasks);
        }
    }
	
    public class EventEmitter<T1, T2> : List<Del<T1, T2>>
    {
        public Task Invoke(T1 arg1, T2 arg2)
        {
            var tasks = new List<Task>();
            foreach (var del in this)
            {
                if (del.Invoke(arg1, arg2) is Task task)
                    tasks.Add(task);
            }
            return tasks.Count == 0 ? Task.CompletedTask : Task.WhenAll(tasks);
        }
    }
	
    public class EventEmitter<T1, T2, T3> : List<Del<T1, T2, T3>>
    {
        public Task Invoke(T1 arg1, T2 arg2, T3 arg3)
        {
            var tasks = new List<Task>();
            foreach (var del in this)
            {
                if (del.Invoke(arg1, arg2, arg3) is Task task)
                    tasks.Add(task);
            }
            return tasks.Count == 0 ? Task.CompletedTask : Task.WhenAll(tasks);
        }
    }
	
}
