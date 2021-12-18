using System;
using UnityEngine;

public abstract class HidingAttribute : PropertyAttribute { }

/// <summary>
/// Hides a field if the bool 'variable' has the state 'state'
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
public class HideIfAttribute : HidingAttribute {
	public readonly string variable;
	public readonly bool state;

	public HideIfAttribute(string variable, bool state, int order = 1) {
		this.variable = variable;
		this.state = state;
		this.order = order;
	}
}

/// <summary>
/// Hides a field if the Object 'variable' is null
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
public class HideIfNullAttribute : HidingAttribute {
	public readonly string variable;
	public readonly bool state;

	public HideIfNullAttribute(string variable, bool state, int order = 1) {
		this.variable = variable;
		this.state = state;
		this.order = order;
	}
}

/// <summary>
/// Hides a field based on it's enum value.
/// use hideIf to specify if the variable must be equal to one of the attributes, or if it must be 
/// unequal to all of the attributes
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
public class HideIfEnumValueAttribute : HidingAttribute {
	public readonly string variable;
	public readonly int[] states;
	public readonly bool hideIfEqual;

	public HideIfEnumValueAttribute(string variable, HideIf hideIf, params int[] states) {
		this.variable = variable;
		this.hideIfEqual = hideIf == HideIf.Equal;
		this.states = states;
		this.order = 1;
	}
}

public enum HideIf {
	Equal,
	NotEqual
}

/// <summary>
/// Hides a field if the function 'method' returns the state 'state'
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
public class HideIfMethodAttribute : HidingAttribute {
	public readonly string method;
	public readonly bool state;

	public HideIfMethodAttribute(string method, bool state, int order = 1) {
		this.method = method;
		this.state = state;
		this.order = order;
	}
}

/// <summary>
/// Hides a field if the property 'property' returns the state 'state'
/// </summary>
[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
public class HideIfPropertyAttribute : HidingAttribute {
	public readonly string property;
	public readonly bool state;

	public HideIfPropertyAttribute(string property, bool state, int order = 1) {
		this.property = property;
		this.state = state;
		this.order = order;
	}
}
