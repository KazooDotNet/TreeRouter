using System;
using System.Collections;
using System.Collections.Generic;

namespace TreeRouter.Http.MultipartForm
{
	internal class Span<T> : IReadOnlyList<T>
	{
		private readonly IReadOnlyList<T> _list;
		private readonly int _start;
		private readonly int _end;

		public Span(IReadOnlyList<T> list, int start, int end)
		{
			_list = list;
			_start = start;
			_end = end;
			if (_list.Count == 0)
				throw new ArgumentException("Cannot create a span on an empty list");
			if (_start < 0)
				throw new ArgumentException("Start cannot be less than 0");
			if (_end >= list.Count)
				throw new ArgumentException("End cannot be greater than list length");
			if (_start > _end)
				throw new ArgumentException("Start cannot be greater than end");
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (var i = _start; i <= _end; i++)
				yield return this[i];
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public int Count => _end - _start + 1;

		public T this[int index] => _list[index + _start];
	}
}
