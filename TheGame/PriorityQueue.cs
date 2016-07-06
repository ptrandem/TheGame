using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheGame
{
    public class PriorityQueue<T>
    {
        private List<PrioritizedQueue<T>> _queues;
        public PriorityQueue()
        {
            _queues = new List<PrioritizedQueue<T>>();
        }

        public void Enqueue(T item, int priority)
        {
            var list = _queues.FirstOrDefault(x => x.Priority == priority);
            if(list == null)
            {
                list = new PrioritizedQueue<T>(priority);
                _queues.Add(list);
            }

            list.Enqueue(item);
        }

        public bool Any()
        {
            foreach(var q in _queues)
            {
                if(q.Any())
                {
                    return true;
                }
            }

            return false;
        }

        public T Dequeue()
        {
            var topListWithAny = _queues.OrderBy(x => x.Priority).FirstOrDefault(x => x.Any());
            return topListWithAny.Dequeue();
        }

        public bool IsEnqueued(Func<T, bool> predicate)
        {
            foreach(var q in _queues)
            {
                var any = q.Any(predicate);
                if (any) return true;
            }

            return false;
        }

        public int Count
        {
            get { return _queues.Sum(x => x.Count); }
        }

        private class PrioritizedQueue<QT> : Queue<QT>
        {
            public int Priority { get; set; }
            public PrioritizedQueue(int priority)
            {
                Priority = priority;
            }
        }
    }

    
}
