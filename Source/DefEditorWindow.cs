// Copyright (c) 2023 bradson
// This Source Code Form is subject to the terms of the MIT license.
// If a copy of the license was not distributed with this file,
// You can obtain one at https://opensource.org/licenses/MIT/.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
#if !V1_4
using LudeonTK;
#endif

namespace AdaptiveStorage;

/// <summary>
/// Mainly a copy of Verse.EditWindow_DefEditor. Doesn't save anything
/// Changes:
/// - No longer crashes when dealing with recursion
/// - Greatly improved performance when displaying large Defs
/// - Relatively fast startup time when dealing with recursion
/// - Better error handling for issues caused by Def.DoEditWidgets()
/// - Support for editing of more value types: Enum?, Vector2, Vector3, Color, IntRange, IntVec2, IntVec3, Rot4
/// - Support for displaying content stored in arrays
/// </summary>
public class DefEditorWindow : EditWindow
{
	public const float TOP_AREA_HEIGHT = 16f;
	public const float EXTRA_SCROLL_HEIGHT = 200f;

	public DefEditorWindow(Def def)
	{
		Def = def;
		closeOnAccept = false;
		closeOnCancel = false;
		optionalTitle = def.ToString();
	}

	public override void DoWindowContents(Rect inRect)
	{
		if (Event.current.type == EventType.KeyDown
			&& Event.current.keyCode is KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Escape)
		{
			UI.UnfocusCurrentControl();
		}

		Rect rect = new(0f, 0f, inRect.width, 16f);
		LabelColumnWidth
#if V1_4
			= Widgets.HorizontalSlider_NewTemp(
#else
			= Widgets.HorizontalSlider(
#endif
				rect, LabelColumnWidth, 0f, inRect.width);
		
		var outRect = inRect.AtZero();
		outRect.yMin += 16f;
		Rect rect2 = new(0f, 0f, outRect.width - 16f, ViewHeight);
		ScrollPosition = outRect.BeginScrollView(ScrollPosition, rect2);
		Listing_TreeDefs listing_TreeDefs = new(LabelColumnWidth);
		listing_TreeDefs.Begin(rect2);
		var node = EditTreeNodeDatabase.RootOf(Def);
		listing_TreeDefs.ContentLines(node, 0, ScrollPosition.y, outRect.height);
		listing_TreeDefs.End();
		if (Event.current.type == EventType.Layout)
			ViewHeight = listing_TreeDefs.CurHeight + 200f;

		Widgets.EndScrollView();
	}

	public override Vector2 InitialSize => new(400f, 600f);
	public override bool IsDebug => true;
	public Def Def { get; }
	public float ViewHeight { get; set; }
	public float LabelColumnWidth { get; set; } = 140f;
	public Vector2 ScrollPosition { get; set; }
}

public static class EditTreeNodeDatabase
{
	public static TreeNode_Editor RootOf(object obj)
	{
		for (var i = 0; i < Roots.Count; i++)
		{
			if (Roots[i].Obj == obj)
				return Roots[i];
		}

		var treeNode_Editor = TreeNode_Editor.NewRootNode(obj);
		Roots.Add(treeNode_Editor);
		return treeNode_Editor;
	}

	public static List<TreeNode_Editor> Roots { get; } = [];
}

public class Listing_TreeDefs(float labelColumnWidth) : Listing_Tree
{
	public void ContentLines(TreeNode_Editor node, int indentLevel, float scrollPosition, float viewHeight)
	{
		node.DoSpecialPreElements(this);

		if (node.children is null)
			node.RebuildChildNodes();

		if (node.children is null)
		{
			var message = node.nestDepth > 14
				? $"{node} has reached its end. Stop doing that."
				: $"{node} children is null.";
			Log.ErrorOnce(message, message.GetHashCode());
			return;
		}

		for (var i = 0; i < node.children.Count; i++)
			Node((TreeNode_Editor)node.children[i], indentLevel, 64, scrollPosition, viewHeight);
	}

	public void Node(TreeNode_Editor node, int indentLevel, int openMask, float scrollPosition, float viewHeight)
	{
		var nodeHeight = node.Height;
		if (viewHeight.ShouldSkipForScrollView(nodeHeight, curY, scrollPosition) && nodeHeight >= 0f)
		{
			curY += nodeHeight;
			return;
		}

		var startingY = curY;
		if (node.NodeType == EditTreeNodeType.TerminalValue)
		{
			node.DoSpecialPreElements(this);
			OpenCloseWidget(node, indentLevel, openMask);
			NodeLabelLeft(node, indentLevel);
			WidgetRow widgetRow = new(LabelWidth, curY);
			ControlButtonsRight(node, widgetRow);
			ValueEditWidgetRight(node, widgetRow.FinalX);
			EndLine();
		}
		else
		{
			OpenCloseWidget(node, indentLevel, openMask);
			NodeLabelLeft(node, indentLevel);
			WidgetRow widgetRow2 = new(LabelWidth, curY);
			ControlButtonsRight(node, widgetRow2);
			ExtraInfoText(node, widgetRow2);
			EndLine();
			if (IsOpen(node, openMask))
				ContentLines(node, indentLevel + 1, scrollPosition, viewHeight);

			if (node.NodeType == EditTreeNodeType.ListRoot)
				node.CheckLatentDelete();
		}

		node.Height = curY - startingY;
	}

	public static void ControlButtonsRight(TreeNode_Editor node, WidgetRow widgetRow)
	{
		if (node.HasNewButton
			&& widgetRow.ButtonIcon(TexButton.NewItem))
		{
			void AddAction(object o)
			{
				node.OwningField.SetValue(node.ParentObj, o);
				((TreeNode_Editor)node.parentNode).RebuildChildNodes();
			}

			MakeCreateNewObjectMenu(node, node.OwningField!.FieldType, AddAction);
		}

		if (node.NodeType == EditTreeNodeType.ListRoot
			&& widgetRow.ButtonIcon(TexButton.Add)
			&& node.Obj is { } obj)
		{
			var type = obj.GetType();
			var baseType = type.IsGenericType
				? type.GetGenericArguments()[0]
				: type.GetElementType();

			void AddAction2(object o) => ((IList)node.Obj).Add(o);

			MakeCreateNewObjectMenu(node, baseType, AddAction2);
		}

		if (node.HasDeleteButton && widgetRow.ButtonIcon(
#if V1_4
			TexButton.DeleteX,
#else
			TexButton.Delete,
#endif
			null, GenUI.SubtleMouseoverColor))
		{
			node.Delete();
		}
	}

	public static void ExtraInfoText(TreeNode_Editor node, WidgetRow widgetRow)
	{
		if (node is not { ExtraInfoText: not "" and var extraInfoText })
			return;

		widgetRow.StyledLabel(extraInfoText, extraInfoText == "null" ? NullInfoLabelStyle : ExtraInfoLabelStyle);
	}

	public void NodeLabelLeft(TreeNode_Editor node, int indentLevel)
	{
		var tipText = "";
		if (node.OwningField is { } owningField)
		{
			var array = (DescriptionAttribute[])owningField.GetCustomAttributes(typeof(DescriptionAttribute),
				inherit: true);
			if (array.Length != 0)
				tipText = array[0].description;
		}

		LabelLeft(node.LabelText, tipText, indentLevel);
	}

	public static void MakeCreateNewObjectMenu(TreeNode_Editor owningNode, Type? baseType, Action<object> addAction)
	{
		var list = baseType.InstantiableDescendantsAndSelf().ToList();
		List<FloatMenuOption> list2 = [];
		foreach (var item in list)
		{
			var creatingType = item;

			void Action()
			{
				owningNode.SetOpen(-1, val: true);
				var obj = creatingType != typeof(string) ? Activator.CreateInstance(creatingType) : "";
				addAction(obj);
				owningNode.RebuildChildNodes();
			}

			list2.Add(new(item.ToString(), Action));
		}

		Find.WindowStack.Add(new FloatMenu(list2));
	}

	public void ValueEditWidgetRight(TreeNode_Editor node, float leftX)
	{
		if (node.NodeType != EditTreeNodeType.TerminalValue)
		{
			throw new ArgumentException(
				$"ValueEditWidgetRight argument must be of type TerminalValue. Got {node.NodeType} instead.",
				nameof(node));
		}

		Rect rect = new(leftX, curY, ColumnWidth - leftX, lineHeight);
		var obj = node.Value;
		var objectType = node.ObjectType;
		if (objectType == typeof(string))
		{
			var text = obj as string;
			var multiLine = false;
			if (text is not null && text.Length > 10)
			{
				for (var width = text.GetWidthCached(); width > rect.width; width -= rect.width)
				{
					rect.height += lineHeight;
					curY += lineHeight;
					multiLine = true;
				}
			}

			var text2 = text ?? "";
			var b = text2;
			text2 = multiLine ? rect.TextArea(text2) : rect.TextField(text2);
			if (text2 != b)
				text = text2;
			
			obj = text;
		}
		else if (objectType == typeof(bool))
		{
			var checkOn = (bool)obj!;
			Widgets.Checkbox(new(rect.x, rect.y), ref checkOn, lineHeight);
			obj = checkOn;
		}
		else if (objectType == typeof(bool?))
		{
			var flag = obj as bool?;
			if (Widgets.ButtonText(rect, flag is { } value ? value.ToString() : "null"))
			{
				List<FloatMenuOption> list = [];
				
				for (var i = 0; i < 3; i++)
				{
					var localIndex = i;
					list.Add(new(localIndex switch
						{
							0 => "true",
							1 => "false",
							_ => "null"
						},
						() => node.Value = localIndex switch
						{
							0 => true,
							1 => false,
							_ => null
						}));
				}

				Find.WindowStack.Add(new FloatMenu(list));
			}
		}
		else if (objectType == typeof(int))
		{
			var int32 = (int)obj!;
			if (IntEditField(rect, ref int32))
				obj = int32;
		}
		else if (objectType == typeof(float))
		{
			var array = (EditSliderRangeAttribute[])node.OwningField!.GetCustomAttributes(
				typeof(EditSliderRangeAttribute), inherit: true);

			var value = (float)obj!;

			if (array.Length != 0)
			{
				value
#if V1_4
					= Widgets.HorizontalSlider_NewTemp(
#else
					= Widgets.HorizontalSlider(
#endif
						new(LabelWidth + 60f + 4f, curY, EditAreaWidth - 60f - 8f, lineHeight),
						value, array[0].min, array[0].max);

				obj = value;
				rect.width = 60f;
			}

			if (FloatEditField(rect, ref value))
				obj = value;
		}
		else if (objectType.IsEnum)
		{
			if (Widgets.ButtonText(rect, obj!.ToString()))
			{
				List<FloatMenuOption> list = [];
				
				foreach (var value2 in Enum.GetValues(objectType))
					list.Add(new(value2.ToString(), () => node.Value = value2));
				
				Find.WindowStack.Add(new FloatMenu(list));
			}
		}
		else if (objectType.IsGenericType
			&& objectType.GetGenericArguments()[0] is { IsEnum: true } innerType) //for Nullable<Enum>
		{
			if (Widgets.ButtonText(rect, obj is null ? "null" : obj.ToString()))
			{
				List<FloatMenuOption> list = [];
				
				foreach (var value2 in Enum.GetValues(innerType))
					list.Add(new(value2.ToString(), () => node.Value = value2));
				
				list.Add(new("null", () => node.Value = null));
				
				Find.WindowStack.Add(new FloatMenu(list));
			}
		}
		else if (objectType == typeof(FloatRange))
		{
			var sliderMin = 0f;
			var sliderMax = 100f;
			var array2 = (EditSliderRangeAttribute[])node.OwningField!.GetCustomAttributes(
				typeof(EditSliderRangeAttribute), inherit: true);

			if (array2.Length != 0)
			{
				sliderMin = array2[0].min;
				sliderMax = array2[0].max;
			}

			var fRange = (FloatRange)obj!;
			Widgets.FloatRangeWithTypeIn(rect, rect.GetHashCode(), ref fRange, sliderMin, sliderMax);
			obj = fRange;
		}
		else if (objectType == typeof(Vector2) || objectType == typeof(Vector2?))
		{
			var vector2 = obj is null ? Vector2.zero : (Vector2)obj;
			rect.width /= 2f;
			if (FloatEditField(rect, ref vector2.x))
				obj = vector2;
			
			rect.x += rect.width;
			if (FloatEditField(rect, ref vector2.y))
				obj = vector2;
		}
		else if (objectType == typeof(Vector3) || objectType == typeof(Vector3?))
		{
			var vector3 = obj is null ? Vector3.zero : (Vector3)obj;
			rect.width /= 3f;
			if (FloatEditField(rect, ref vector3.x))
				obj = vector3;
			
			rect.x += rect.width;
			if (FloatEditField(rect, ref vector3.y))
				obj = vector3;
			
			rect.x += rect.width;
			if (FloatEditField(rect, ref vector3.z))
				obj = vector3;
		}
		else if (objectType == typeof(Color) || objectType == typeof(Color?))
		{
			var color = obj is null ? default : (Color)obj;
			rect.width /= 4f;
			if (FloatEditField(rect, ref color.r))
				obj = color;
			
			rect.x += rect.width;
			if (FloatEditField(rect, ref color.g))
				obj = color;
			
			rect.x += rect.width;
			if (FloatEditField(rect, ref color.b))
				obj = color;
			
			rect.x += rect.width;
			if (FloatEditField(rect, ref color.a))
				obj = color;
		}
		else if (objectType == typeof(IntRange))
		{
			var intRange = (IntRange)obj!;
			rect.width /= 4f;
			IntEditField(rect, ref intRange.min);
			rect.x += rect.width;
			rect.width *= 2f;
			Widgets.IntRange(rect, rect.GetHashCode(), ref intRange, max: intRange.max > 0 ? intRange.max * 2 : 10);
			rect.x += rect.width;
			rect.width /= 2;
			IntEditField(rect, ref intRange.max);
			obj = intRange;
		}
		else if (objectType == typeof(IntVec2))
		{
			var intVec2 = (IntVec2)obj!;
			rect.width /= 2f;
			if (IntEditField(rect, ref intVec2.x))
				obj = intVec2;
			
			rect.x += rect.width;
			if (IntEditField(rect, ref intVec2.z))
				obj = intVec2;
		}
		else if (objectType == typeof(IntVec3))
		{
			var intVec3 = (IntVec3)obj!;
			rect.width /= 3f;
			if (IntEditField(rect, ref intVec3.x))
				obj = intVec3;
			
			rect.x += rect.width;
			if (IntEditField(rect, ref intVec3.y))
				obj = intVec3;
			
			rect.x += rect.width;
			if (IntEditField(rect, ref intVec3.z))
				obj = intVec3;
		}
		else if (objectType == typeof(Rot4) || objectType == typeof(Rot4?))
		{
			var rot4 = obj as Rot4?;
			if (Widgets.ButtonText(rect, rot4 is { } value ? value.ToStringWord() : "null"))
			{
				List<FloatMenuOption> list = [];
				foreach (var value2 in Enum.GetValues(typeof(Rot4Enum)))
				{
					var localVal = value2;
					list.Add(new(value2.ToString(), () => node.Value = new Rot4((byte)localVal)));
				}
				
				if (objectType == typeof(Rot4?))
					list.Add(new("null", () => node.Value = null));

				Find.WindowStack.Add(new FloatMenu(list));
			}
		}
		else
		{
			rect.Label("uneditable value type", GrayLabelStyle);
		}

		node.Value = obj;
	}

	public static bool FloatEditField(Rect rect, ref float value)
	{
		if (!float.TryParse(rect.TextField(value.ToString(CultureInfo.InvariantCulture)), out var newResult)
			|| ((Math.Abs(value - newResult) < 0.001f) && (float.IsFinite(value) == float.IsFinite(newResult))))
		{
			return false;
		}

		value = newResult;
		return true;
	}

	public static bool IntEditField(Rect rect, ref int value)
	{
		if (!int.TryParse(rect.TextField(value.ToString()), out var newResult))
			return false;

		value = newResult;
		return true;

	}

	[UsedImplicitly(ImplicitUseTargetFlags.Members)]
	public enum Rot4Enum : byte
	{
		North,
		East,
		South,
		West
	}

	protected override float LabelWidth => LabelWidthInt;

	public float GetLabelWidth() => LabelWidth;

	public static GUIStyle NullInfoLabelStyle { get; }
		= new(UIHelpers.LabelStyle) { normal = { textColor = new(1f, 0.6f, 0.6f, 0.5f) } };

	public static GUIStyle ExtraInfoLabelStyle { get; }
		= new(UIHelpers.LabelStyle) { normal = { textColor = new(1f, 1f, 1f, 0.5f) } };

	public static GUIStyle GrayLabelStyle { get; }
		= new(UIHelpers.LabelStyle) { normal = { textColor = new(1f, 1f, 1f, 0.4f) } };

	private float LabelWidthInt { get; } = labelColumnWidth;
}

public class TreeNode_Editor : TreeNode
{
	public static TreeNode_Editor NewRootNode(object rootObj)
	{
		if (rootObj.GetType().IsValueEditable())
		{
			throw new ArgumentException("Tried to make TreeNode_Editor for a value that is not editable",
				nameof(rootObj));
		}

		TreeNode_Editor treeNode_Editor = new() { OwningField = null, Obj = rootObj, nestDepth = 0 };
		treeNode_Editor.RebuildChildNodes();
		treeNode_Editor.InitiallyCacheData();
		return treeNode_Editor;
	}

	public static TreeNode_Editor NewChildNodeFromField(TreeNode_Editor parent, FieldInfo fieldInfo)
	{
		TreeNode_Editor treeNode_Editor = new()
		{
			parentNode = parent, nestDepth = parent.nestDepth + 1, OwningField = fieldInfo
		};
		if (!fieldInfo.FieldType.IsValueEditable())
		{
			treeNode_Editor.Obj = fieldInfo.GetValue(parent.Obj);
//			treeNode_Editor.RebuildChildNodes();
		}

		treeNode_Editor.InitiallyCacheData();
		return treeNode_Editor;
	}

	public static TreeNode_Editor NewChildNodeFromListItem(TreeNode_Editor parent, IList listObject, int listIndex)
	{
		TreeNode_Editor treeNode_Editor = new()
		{
			parentNode = parent, nestDepth = parent.nestDepth + 1, OwningIndex = listIndex
		};
		var type = listObject.GetType();

		if (!((type.IsGenericType && type.GetGenericArguments()[0].IsValueEditable())
			|| (type.HasElementType && type.GetElementType().IsValueEditable())))
		{
			treeNode_Editor.Obj = listObject[listIndex];
//			treeNode_Editor.RebuildChildNodes();
		}

		treeNode_Editor.InitiallyCacheData();
		return treeNode_Editor;
	}

	public TreeNode_Editor()
	{
	}

	public TreeNode_Editor(TreeNode_Editor original, TreeNode_Editor parentNode, int nestDepth)
	{
		this.parentNode = parentNode;
		this.nestDepth = nestDepth;
		OwningIndex = original.OwningIndex;
		OwningField = original.OwningField;
		Obj = original.Obj;
		NodeType = original.NodeType;
		EditWidgetsMethod = original.EditWidgetsMethod;
		children = CopyChildren(original);
	}

	public List<TreeNode>? CopyChildren(TreeNode original)
	{
		if (nestDepth > 10 || original.children is null)
			return null;

		List<TreeNode> newList = [];
		for (var i = 0; i < original.children.Count; i++)
			newList.Add(new TreeNode_Editor((TreeNode_Editor)original.children[i], this, nestDepth + 1));
		return newList;
	}

	public void InitiallyCacheData()
	{
		NodeType = Obj is IList
			? EditTreeNodeType.ListRoot
			: ObjectType.IsValueEditable()
				? EditTreeNodeType.TerminalValue
				: EditTreeNodeType.ComplexObject;

		if (Obj?.GetType().GetMethod("DoEditWidgets") is { } method)
			EditWidgetsMethod = method;
	}

	public void RebuildChildNodes()
	{
		if (Obj is null || nestDepth > 15)
			return;

		if (Obj == (parentNode as TreeNode_Editor)?.Obj)
			children = CopyChildren(parentNode);
		else if (Obj == (parentNode?.parentNode as TreeNode_Editor)?.Obj)
			children = CopyChildren(parentNode!.parentNode);

		children = [];
		if (Obj is IList o)
		{
			var num = o.Count;
			for (var i = 0; i < num; i++)
				children.Add(NewChildNodeFromListItem(this, o, i));
			return;
		}

		var objType = Obj.GetType();
		foreach (var item3 in objType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.OrderByDescending(f => InheritanceDistanceBetween(objType, f.DeclaringType)))
		{
			if (item3.GetCustomAttributes(typeof(UnsavedAttribute), inherit: true).Length == 0
				&& item3.GetCustomAttributes(typeof(EditorHiddenAttribute), inherit: true).Length == 0)
				children.Add(NewChildNodeFromField(this, item3));
		}
	}

	public static int InheritanceDistanceBetween(Type childType, Type? parentType)
	{
		var type = childType;
		var num = 0;
		do
		{
			if (type == parentType)
				return num;

			type = type.BaseType;
			num++;
		}
		while (type is not null);

		Log.Error($"{childType} is not a subclass of {parentType}");
		return -1;
	}

	public void CheckLatentDelete()
	{
		if (IndexToDelete < 0)
			return;

		(Obj as IList)?.RemoveAt(IndexToDelete);
		RebuildChildNodes();
		IndexToDelete = -1;
	}

	public void Delete()
	{
		if (OwningField is { } oF)
		{
			oF.SetValue(Obj, null);
		}
		else
		{
			((TreeNode_Editor)parentNode).IndexToDelete
				= IsListItem ? OwningIndex : throw new InvalidOperationException();
		}
	}

	public void DoSpecialPreElements(Listing_TreeDefs listing)
	{
		if (Obj is null)
			return;

		if (EditWidgetsMethod is not null)
		{
			var widgetRow = listing.StartWidgetsRow(nestDepth);
			try
			{
				EditWidgetsMethod.Invoke(Obj, [widgetRow]);
			}
			catch (Exception ex)
			{
				var message = new StringBuilder("Encountered an exception when trying to handle ");
				message.Append(Obj);
				message.Append('.');
				if (Find.CurrentMap is null)
				{
					message.Append($" A few methods don't work from the main menu and require a map to be loaded. This "
						+ $"might be one of them.");
				}
				message.AppendLine();
				message.Append(ex);

				Log.Error(message.ToString());
			}
		}

		if (Obj is not Editable editable)
			return;

		foreach (var item in editable.ConfigErrors())
			listing.StyledInfoText(item, SpecialInfoTextStyle);
	}

	public override string ToString()
	{
		StringBuilder text = new("EditTreeNode(");
		if (ParentObj is not null)
		{
			text.Append("owningObj=");
			text.Append(ParentObj);
		}

		if (OwningField is not null)
		{
			text.Append(" owningField=");
			text.Append(OwningField);
		}

		if (OwningIndex >= 0)
		{
			text.Append(" owningIndex=");
			text.Append(OwningIndex);
		}

		return text.Append(')').ToString();
	}

	public override bool Openable
		=> Obj is not null
			&& NodeType != EditTreeNodeType.TerminalValue
			&& !(NodeType == EditTreeNodeType.ListRoot && ((IList)Obj).Count == 0);

	public object? Value
	{
		get
			=> OwningField is not null
				? OwningField.GetValue(ParentObj)
				: IsListItem
					? ((IList)ListRootObject)[OwningIndex]
					: throw new InvalidOperationException();
		set
		{
			if (OwningField is not null)
				OwningField.SetValue(ParentObj, value);
			else if (IsListItem)
				((IList)ListRootObject)[OwningIndex] = value;
			else
				throw new InvalidOperationException();
		}
	}

	public Type ObjectType
		=> _objectType
			??= OwningField is not null
				? OwningField.FieldType
				: IsListItem && ListRootObject.GetType() is var type
					? (type.IsGenericType ? type.GetGenericArguments()[0] :
						type.HasElementType ? type.GetElementType()! :
						Obj?.GetType() ?? throw new InvalidOperationException())
					: Obj?.GetType() ?? throw new InvalidOperationException();

	public string ExtraInfoText
		=> Obj is null
			? "null"
			: Obj.GetType().HasAttribute<EditorShowClassNameAttribute>()
				? Obj.GetType().Name
				: Obj is IList iList
					? $"({iList.Count} {((iList.Count == 1) ? "element" : "elements")})"
					: "";

	public string LabelText
		=> OwningField != null
			? OwningField.Name
			: IsListItem
				? OwningIndex.ToString()
				: ObjectType.Name;

	public bool HasNewButton
	{
		get
		{
			switch (_hasNewButton)
			{
				case 1:
					return true;
				case 2:
					return false;
			}

			if ((NodeType == EditTreeNodeType.ComplexObject && Obj is null)
				|| (OwningField is not null && OwningField.FieldType.HasAttribute<EditorReplaceableAttribute>()))
			{
				_hasNewButton = 1;
				return true;
			}
			else
			{
				_hasNewButton = 2;
				return true;
			}
		}
	}

	public bool HasDeleteButton
	{
		get
		{
			switch (_hasDeleteButton)
			{
				case 1:
					return true;
				case 2:
					return false;
			}

			if (IsListItem || (OwningField is { } field && field.FieldType.HasAttribute<EditorNullableAttribute>()))
			{
				_hasDeleteButton = 1;
				return true;
			}
			else
			{
				_hasDeleteButton = 2;
				return true;
			}
		}
	}

	public static GUIStyle SpecialInfoTextStyle { get; }
		= new(UIHelpers.LabelStyle) { normal = { textColor = new(1f, 0.5f, 0.5f, 1f) } };

	public float Height { get; set; } = -1f;
	public bool HasContentLines => NodeType != EditTreeNodeType.TerminalValue;
	
	[MemberNotNullWhen(true, nameof(ListRootObject))]
	public bool IsListItem => OwningIndex >= 0;
	
	public object? ListRootObject => ParentObj;
	public object? ParentObj => ((TreeNode_Editor)parentNode).Obj;
	public object? Obj { get; set; }
	public FieldInfo? OwningField { get; init; }
	public int OwningIndex { get; init; } = -1;
	public MethodInfo? EditWidgetsMethod { get; set; }
	public EditTreeNodeType NodeType { get; set; }
	public int IndexToDelete { get; set; } = -1;

	private Type? _objectType;
	private byte _hasNewButton;
	private byte _hasDeleteButton;
}