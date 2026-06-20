using System.Collections;
using System.Collections.Generic;

public class mVector
{
	private List<object> a;

	public mVector()
	{
		a = new List<object>();
	}

	public mVector(string s)
	{
		a = new List<object>();
	}

	public mVector(ArrayList al)
	{
		a = new List<object>(al.Count);
		for (int i = 0; i < al.Count; i++)
		{
			a.Add(al[i]);
		}
	}

	public void addElement(object o)
	{
		a.Add(o);
	}

	public bool contains(object o)
	{
		return a.Contains(o);
	}

	public int size()
	{
		if (a == null)
		{
			return 0;
		}
		return a.Count;
	}

	public object elementAt(int index)
	{
		if (index > -1 && index < a.Count)
		{
			return a[index];
		}
		return null;
	}

	public void set(int index, object obj)
	{
		if (index > -1 && index < a.Count)
		{
			a[index] = obj;
		}
	}

	public void setElementAt(object obj, int index)
	{
		if (index > -1 && index < a.Count)
		{
			a[index] = obj;
		}
	}

	public int indexOf(object o)
	{
		return a.IndexOf(o);
	}

	public void removeElementAt(int index)
	{
		if (index > -1 && index < a.Count)
		{
			a.RemoveAt(index);
		}
	}

	public void swapRemoveAt(int index)
	{
		if (index > -1 && index < a.Count)
		{
			int last = a.Count - 1;
			if (index < last)
			{
				a[index] = a[last];
			}
			a.RemoveAt(last);
		}
	}

	public void removeElement(object o)
	{
		a.Remove(o);
	}

	public void removeAllElements()
	{
		a.Clear();
	}

	public void insertElementAt(object o, int i)
	{
		a.Insert(i, o);
	}

	public object firstElement()
	{
		return a[0];
	}

	public object lastElement()
	{
		return a[a.Count - 1];
	}
}
