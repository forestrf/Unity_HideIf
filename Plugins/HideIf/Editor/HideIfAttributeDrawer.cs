using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using HideIf_Utilities;

public abstract class HidingAttributeDrawer : PropertyDrawer {
	protected abstract bool ShouldDraw(SerializedProperty property);

	/// <summary>
	/// Type to PropertyDrawer types for that type
	/// </summary>
	static Dictionary<Type, Type> typeToDrawerType;

	/// <summary>
	/// PropertyDrawer types to instances of that type 
	/// </summary>
	static Dictionary<Type, PropertyDrawer> drawerTypeToDrawerInstance;

	static readonly FieldInfo m_FieldInfo = typeof(PropertyDrawer).GetField("m_FieldInfo", BindingFlags.NonPublic | BindingFlags.Instance);
	static readonly FieldInfo m_Attribute = typeof(PropertyDrawer).GetField("m_Attribute", BindingFlags.NonPublic | BindingFlags.Instance);
	static readonly FieldInfo targetType = typeof(CustomPropertyDrawer).GetField("m_Type", BindingFlags.Instance | BindingFlags.NonPublic);
	static readonly FieldInfo useForChildren = typeof(CustomPropertyDrawer).GetField("m_UseForChildren", BindingFlags.Instance | BindingFlags.NonPublic);
	static readonly Type propertyDrawerType = typeof(PropertyDrawer);
	static readonly Type[] types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()).Where(type => propertyDrawerType.IsAssignableFrom(type)).ToArray();

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		if (!ShouldDraw(property)) return;

		var drawer = GetDrawer(property);

		if (drawer != null) {
			drawer.OnGUI(position, property, label);
		}
		else {
			EditorGUI.PropertyField(position, property, label, true);
		}
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		//Even if the property height is 0, the property gets margins of 1 both up and down.
		//So to truly hide it, we have to hack a height of -2 to counteract that!
		if (!ShouldDraw(property))
			return -2;

		var drawer = GetDrawer(property);

		if (drawer != null) {
			return drawer.GetPropertyHeight(property, label);
		}
		return EditorGUI.GetPropertyHeight(property, label, true);
	}

	PropertyDrawer GetDrawer(SerializedProperty property) {
		if (typeToDrawerType == null)
			PopulateTypeToDrawer();

		Type drawerType;

		var customPropertyAttr = fieldInfo.GetCustomAttributes(typeof(PropertyAttribute), true).ToArray();
		int attIndexOf = Array.IndexOf(customPropertyAttr, attribute);
		for (int i = attIndexOf + 1; i < customPropertyAttr.Length; i++) {
			var att = customPropertyAttr[i];

			if (typeToDrawerType.TryGetValue(att.GetType(), out drawerType)) {
				var drawer = drawerTypeToDrawerInstance.GetOrAdd(drawerType, () => CreateDrawerInstance(drawerType));
				var obj = GetHoldingObject(property);
				m_FieldInfo.SetValue(drawer, fieldInfo);
				m_Attribute.SetValue(drawer, att);
				return drawer;
			}

			/*
			// Idk what to do about this
			//var targetedType = (Type) targetType.GetValue(att);
			var useThisForChildren = (bool) useForChildren.GetValue(propertyDrawer);
			if (useThisForChildren) {
				var childTypes = types.Where(t => targetedType.IsAssignableFrom(t) && t != targetedType);
				foreach (var childType in childTypes) {
					typeToDrawerType[childType] = member;
				}
			}
			*/
		}

		var typeOfProp = Utilities.GetTargetObjectOfProperty(property).GetType();
		if (typeToDrawerType.TryGetValue(typeOfProp, out drawerType)) {
			var drawer = drawerTypeToDrawerInstance.GetOrAdd(drawerType, () => CreateDrawerInstance(drawerType));
			return drawer;
		}

		return null;
	}

	private PropertyDrawer CreateDrawerInstance(Type drawerType) {
		return (PropertyDrawer) Activator.CreateInstance(drawerType);
	}

	private void PopulateTypeToDrawer() {
		typeToDrawerType = new Dictionary<Type, Type>();
		drawerTypeToDrawerInstance = new Dictionary<Type, PropertyDrawer>();

		foreach (Type type in types) {
			var customPropertyDrawers = type.GetCustomAttributes(true).OfType<CustomPropertyDrawer>().ToArray();
			foreach (var propertyDrawer in customPropertyDrawers) {
				var targetedType = (Type) targetType.GetValue(propertyDrawer);
				typeToDrawerType[targetedType] = type;

				var useThisForChildren = (bool) useForChildren.GetValue(propertyDrawer);
				if (useThisForChildren) {
					var childTypes = types.Where(t => targetedType.IsAssignableFrom(t) && t != targetedType);
					foreach (var childType in childTypes) {
						typeToDrawerType[childType] = type;
					}
				}
			}
		}
	}

	protected static string GetParentPathProperty(SerializedProperty property) {
		string path = property.propertyPath;
		return path.Substring(0, path.LastIndexOf(".") + 1);
	}
	protected static SerializedProperty GetProperty(SerializedProperty property, string variable) {
		return property.serializedObject.FindProperty(GetParentPathProperty(property) + variable);
	}
	protected static object GetHoldingObject(SerializedProperty property) {
		var path = GetParentPathProperty(property);
		if (path[path.Length - 1] == '.') path = path.Substring(0, path.Length - 1);
		if (string.IsNullOrEmpty(path)) return property.serializedObject.targetObject;
		else return Utilities.GetTargetObjectOfProperty(property.serializedObject.FindProperty(path));
	}
}

[CustomPropertyDrawer(typeof(HideIfAttribute))]
public class HideIfAttributeDrawer : HidingAttributeDrawer {
	protected override bool ShouldDraw(SerializedProperty property) {
		var att = (HideIfAttribute) attribute;
		var prop = GetProperty(property, att.variable);
		if (prop == null) return true;

		return prop.boolValue != att.state;
	}
}

[CustomPropertyDrawer(typeof(HideIfNullAttribute))]
public class HideIfNullAttributeDrawer : HidingAttributeDrawer {
	protected override bool ShouldDraw(SerializedProperty property) {
		var att = (HideIfNullAttribute) attribute;
		var prop = GetProperty(property, att.variable);
		if (prop == null) return true;

		return prop.objectReferenceValue != null ^ att.state;
	}
}

[CustomPropertyDrawer(typeof(HideIfEnumValueAttribute))]
public class HideIfEnumValueAttributeDrawer : HidingAttributeDrawer {
	protected override bool ShouldDraw(SerializedProperty property) {
		var att = (HideIfEnumValueAttribute) attribute;
		var enumProp = GetProperty(property, att.variable);
		if (enumProp == null) return true;

		var states = att.states;

		//enumProp.enumValueIndex gives the order in the enum list, not the actual enum value
		bool equal = states.Contains(enumProp.intValue);

		return equal != att.hideIfEqual;
	}
}

[CustomPropertyDrawer(typeof(HideIfMethodAttribute))]
public class HideIfMethodAttributeDrawer : HidingAttributeDrawer {
	static Dictionary<(Type, string), MethodInfo> typeToMethodInfo = new Dictionary<(Type, string), MethodInfo>();

	protected override bool ShouldDraw(SerializedProperty property) {
		var att = (HideIfMethodAttribute) attribute;
		var obj = GetHoldingObject(property);
		var type = obj.GetType();
		if (!typeToMethodInfo.TryGetValue((type, att.method), out var method)) {
			method = type.GetMethod(att.method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			typeToMethodInfo.Add((type, att.method), method);
		}
		if (method == null) return true;

		return (bool) method.Invoke(obj, null) != att.state;
	}
}

[CustomPropertyDrawer(typeof(HideIfPropertyAttribute))]
public class HideIfPropertyAttributeDrawer : HidingAttributeDrawer {
	static Dictionary<(Type, string), PropertyInfo> typeToPropertyInfo = new Dictionary<(Type, string), PropertyInfo>();

	protected override bool ShouldDraw(SerializedProperty property) {
		var att = (HideIfPropertyAttribute) attribute;
		var obj = GetHoldingObject(property);
		var type = obj.GetType();
		if (!typeToPropertyInfo.TryGetValue((type, att.property), out var method)) {
			method = type.GetProperty(att.property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			typeToPropertyInfo.Add((type, att.property), method);
		}
		if (method == null) return true;

		return (bool) method.GetValue(obj, null) != att.state;
	}
}
