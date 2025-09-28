// src* = https://gist.github.com/andrew-raphael-lukasik/d38a11bf0559a723617b9deaeedb2eac
using UnityEngine;
using UnityEngine.UIElements;

public static class ExtensionMethods_UIToolkit
{

	public static bool Q<T>(this VisualElement root, string name, out T result, params string[] classes)
		where T : VisualElement
	{
		result = root.Find<T>(name, classes);
		return result != null;
	}
	public static bool Q<T>(this VisualElement root, out T result, params string[] classes)
		where T : VisualElement
	{
		result = root.Find<T>(null, classes);
		return result != null;
	}

	public static T For<T>(this VisualElement root, string name, System.Action<T> action, params string[] classes)
		where T : VisualElement
	{
		T result = root.Find<T>(name, classes);
		if (result != null)
		{
			if (action != null)
				action(result);

			int numFound = root.Query<T>(name, classes).ToList().Count;
			if (numFound == 0) Debug.LogWarning($"no <{typeof(T).Name}> name:{name} classes:{JsonUtility.ToJson(classes)} found!");
			else if (numFound != 1) Debug.LogWarning($"number of <{typeof(T).Name}> name:{name} classes:{JsonUtility.ToJson(classes)} found is {numFound} where only 1 was expected");
		}
		return result;
	}

	public static T Find<T>(this VisualElement root, string name, params string[] classes)
		where T : VisualElement
	{
		T result = default(T);
		if (root != null)
		{
			result = UQueryExtensions.Q<T>(root, name, classes);
			if (result == null) Debug.LogWarning($"{root.name}.'{name}'<{typeof(T).Name}> classes:{JsonUtility.ToJson(classes)} not found");
		}
		else Debug.LogWarning($"given {nameof(root)} is null!");
		return result;
	}
	public static T Find<T>(this VisualElement root, params string[] classes)
		where T : VisualElement
		=> Find<T>(root, null, classes);

	public static void Foreach<T>(this VisualElement root, string name, System.Action<T> action, params string[] classes)
		where T : VisualElement
	{
		var query = root.Query<T>(name, classes);
		int numFound = query.ToList().Count;
		if (numFound == 0) Debug.LogWarning($"no <{typeof(T).Name}> name:{name} classes:{JsonUtility.ToJson(classes)} found!");
		else if (numFound != 1) Debug.LogWarning($"number of <{typeof(T).Name}> name:{name} classes:{JsonUtility.ToJson(classes)} found is {numFound} where only 1 was expected");
		query.ForEach(action);
	}
	public static void Foreach<T>(this VisualElement root, System.Action<T> action, params string[] classes)
		where T : VisualElement
		=> Foreach<T>(root, action, classes);

}
